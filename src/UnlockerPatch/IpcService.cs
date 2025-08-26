using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace UnlockerPatch;

public enum IpcStatus
{
    Error = -1,
    None = 0,
    HostAwaiting = 1,
    ClientReady = 2,
    ClientExit = 3,
    HostExit = 4,
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct IpcData
{
    public ulong Address;
    public int Value;
    public IpcStatus Status;
}

public class IpcService : IDisposable
{
    private bool _started = false;
    private nint _pFpsValue = nint.Zero;
    private MemoryMappedFile? _sharedMemory = null;
    private MemoryMappedViewAccessor? _sharedMemoryAccessor = null;
    private ModuleGuard _stubModule = nint.Zero;
    private nint _wndHook = nint.Zero;

    public void Start(int processId, nint pFpsValue)
    {
        if (_started)
            return;

        _pFpsValue = pFpsValue;

        _sharedMemory = MemoryMappedFile.CreateOrOpen("2DE95FDC-6AB7-4593-BFE6-760DD4AB422B", 4096, MemoryMappedFileAccess.ReadWrite);
        _sharedMemoryAccessor = _sharedMemory.CreateViewAccessor();
        Debug.WriteLine("打开内存成功！");
        WriteToSharedMemory(_pFpsValue, 60, IpcStatus.HostAwaiting);

        _stubModule = Native.LoadLibrary("UnlockerStub.dll");
        if (_stubModule == nint.Zero)
        {
            string errorMessage = $@"Failed to load stub module: {Marshal.GetLastWin32Error()}{Environment.NewLine}{Marshal.GetLastPInvokeErrorMessage()}";
            Console.WriteLine(errorMessage, @"Error");
            return;
        }

        var stubWndProc = Native.GetProcAddress(_stubModule, "WndProc");
        var targetWindow = ProcessUtils.GetWindowFromProcessId(processId);
        var threadId = Native.GetWindowThreadProcessId(targetWindow, out uint _);

        _wndHook = Native.SetWindowsHookEx(3, stubWndProc, _stubModule, threadId);
        if (_wndHook == nint.Zero)
        {
            string errorMessage = $@"Failed to set window hook: {Marshal.GetLastWin32Error()}{Environment.NewLine}{Marshal.GetLastPInvokeErrorMessage()}";
            Console.WriteLine(errorMessage, @"Error");
            return;
        }

        if (!Native.PostThreadMessage(threadId, 0, nint.Zero, nint.Zero))
        {
            string errorMessage = $@"Failed to post thread message: {Marshal.GetLastWin32Error()}{Environment.NewLine}{Marshal.GetLastPInvokeErrorMessage()}";
            Console.WriteLine(errorMessage, @"Error");
            return;
        }

        int retryCount = 0;
        while (true)
        {
            _sharedMemoryAccessor.Read(0, out IpcData ipcData);

            if (ipcData.Status == IpcStatus.ClientReady)
                break;

            if (retryCount >= 10)
            {
                Console.WriteLine(@"Failed to start the unlocker.", @"Error");
                return;
            }

            retryCount++;
            Task.Delay(1000).Wait();
        }

        _started = true;
    }

    public void ApplyFpsLimit(int fps)
    {
        if (_pFpsValue == nint.Zero)
            return;

        WriteToSharedMemory(_pFpsValue, fps, IpcStatus.None);
    }

    public void Stop()
    {
        _started = false;
        _pFpsValue = nint.Zero;

        WriteToSharedMemory(nint.Zero, 0, IpcStatus.HostExit);
        Task.Delay(200).Wait();
        Native.UnhookWindowsHookEx(_wndHook);
        Native.FreeLibrary(_stubModule);
    }

    private void WriteToSharedMemory(nint address, int fps, IpcStatus status)
    {
        IpcData ipcData = new()
        {
            Address = (ulong)address,
            Value = fps,
            Status = status
        };

        _sharedMemoryAccessor?.Write(0, ref ipcData);
    }

    public void Dispose()
    {
        Stop();
        _sharedMemoryAccessor?.Dispose();
        _sharedMemory?.Dispose();
    }
}
