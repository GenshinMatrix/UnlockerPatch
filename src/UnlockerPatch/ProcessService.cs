using System.Runtime.InteropServices;

namespace UnlockerPatch;

public class ProcessService()
{
    private nint _gameHandle = nint.Zero;
    private nint _remoteUnityPlayer = nint.Zero;
    private nint _remoteUserAssembly = nint.Zero;
    private int _gamePid = 0;
    private bool _failover = false;
    private nint _pFpsValue = nint.Zero;

    private readonly IpcService _ipcService = new();

    public bool Start()
    {
        if (IsGameRunning())
            return false;

        _failover = false;
        _ipcService.Stop();

        Task.Run(Worker, UnlockerLauncher.Token);
        return true;
    }

    private bool IsGameRunning()
    {
        if (_gameHandle == nint.Zero)
            return false;

        if (!Native.GetExitCodeProcess(_gameHandle, out uint exitCode))
            return false;

        return exitCode == 259;
    }

    private async Task Worker()
    {
        STARTUPINFO si = new();
        PROCESS_INFORMATION pi = new();
        uint creationFlag = false ? 4u : 0u;
        string? gameFolder = Path.GetDirectoryName(UnlockerLauncher.GamePath);
        if (!Native.CreateProcess(UnlockerLauncher.GamePath, UnlockerLauncher.CommandLine, nint.Zero, nint.Zero, false, creationFlag, nint.Zero, gameFolder!, ref si, out pi))
            return;

        _gamePid = pi.dwProcessId;
        _gameHandle = pi.hProcess;

        Native.CloseHandle(pi.hThread);

        SpinWait.SpinUntil(() => ProcessUtils.GetWindowFromProcessId(_gamePid) != nint.Zero);

        if (!SetupData())
            return;

        while (IsGameRunning() && !UnlockerLauncher.Token.IsCancellationRequested)
        {
            ApplyFpsLimit();
            await Task.Delay(1000, UnlockerLauncher.Token);
        }

        if (!IsGameRunning())
        {
            _ipcService.Stop();
            _pFpsValue = nint.Zero;
            _gameHandle = nint.Zero;
            _ipcService.Stop();
            Native.CloseHandle(_gameHandle);
        }
    }

    private void ApplyFpsLimit()
    {
        if (_pFpsValue == nint.Zero)
            return;

        if (!_failover)
        {
            var toWrite = BitConverter.GetBytes(UnlockerLauncher.TargetFps);
            if (!Native.WriteProcessMemory(_gameHandle, _pFpsValue, toWrite, 4, out _) && IsGameRunning())
            {
                if (Marshal.GetLastWin32Error() == 5)
                {
                    _ipcService.Start(_gamePid, _pFpsValue);
                    _failover = true;
                }
            }
        }
        else
        {
            _ipcService.ApplyFpsLimit(UnlockerLauncher.TargetFps);
        }
    }

    private unsafe bool SetupData()
    {
        var gameDir = Path.GetDirectoryName(UnlockerLauncher.GamePath);
        var gameName = Path.GetFileNameWithoutExtension(UnlockerLauncher.GamePath);
        var dataDir = Path.Combine(gameDir!, $"{gameName}_Data");

        var unityPlayerPath = Path.Combine(gameDir!, "UnityPlayer.dll");
        var userAssemblyPath = Path.Combine(dataDir, "Native", "UserAssembly.dll");

        using ModuleGuard pUnityPlayer = Native.LoadLibraryEx(unityPlayerPath, nint.Zero, 32);
        using ModuleGuard pUserAssembly = Native.LoadLibraryEx(userAssemblyPath, nint.Zero, 32);

        if (!pUnityPlayer || !pUserAssembly)
        {
            if (!File.Exists(unityPlayerPath) && !File.Exists(userAssemblyPath))
            {
                if (SetupDataEx())
                    return true;
                goto BAD_PATTERN;
            }
            return false;
        }

        if (!UpdateRemoteModules())
            return false;

        BAD_PATTERN:
        return false;
    }

    private unsafe bool SetupDataEx()
    {
        var gameName = Path.GetFileNameWithoutExtension(UnlockerLauncher.GamePath);
        var remoteExe = ProcessUtils.GetModuleBase(_gameHandle, $"{gameName}.exe");
        if (remoteExe == nint.Zero)
            return false;

        using ModuleGuard pGenshinImpact = Native.LoadLibraryEx(UnlockerLauncher.GamePath, nint.Zero, 32);
        if (!pGenshinImpact)
            return false;

        List<nint> vaResults = ProcessUtils.PatternScanAllOccurrences(pGenshinImpact, "B9 3C 00 00 00 E8");
        if (vaResults.Count == 0)
            return false;

        var localVa = (byte*)vaResults
            .Select(x => x + 5)
            .Select(x => x + *(int*)(x + 1) + 5)
            .FirstOrDefault(x => *(byte*)x == 0xE9);

        if (localVa == null)
            return false;

        while (localVa[0] == 0xE8 || localVa[0] == 0xE9)
            localVa += *(int*)(localVa + 1) + 5;

        localVa += *(int*)(localVa + 2) + 6;
        var rva = localVa - pGenshinImpact.BaseAddress.ToInt64();
        _pFpsValue = (nint)(remoteExe + rva);

        return true;
    }

    private bool UpdateRemoteModules()
    {
        int retries = 0;

        while (true)
        {
            _remoteUnityPlayer = ProcessUtils.GetModuleBase(_gameHandle, "UnityPlayer.dll");
            _remoteUserAssembly = ProcessUtils.GetModuleBase(_gameHandle, "UserAssembly.dll");

            if (_remoteUnityPlayer != nint.Zero && _remoteUserAssembly != nint.Zero)
                break;

            if (retries > 10)
                break;

            Task.Delay(2000, UnlockerLauncher.Token).Wait();
            retries++;
        }

        if (_remoteUnityPlayer == nint.Zero || _remoteUserAssembly == nint.Zero)
            return false;
        return true;
    }
}
