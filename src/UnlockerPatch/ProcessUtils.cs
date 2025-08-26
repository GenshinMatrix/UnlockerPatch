using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace UnlockerPatch;

internal class ProcessUtils
{
    public static string GetProcessPathFromPid(uint pid, out nint processHandle)
    {
        var hProcess = Native.OpenProcess(
            ProcessAccess.QUERY_LIMITED_INFORMATION |
            ProcessAccess.TERMINATE |
            StandardAccess.SYNCHRONIZE, false, pid);

        processHandle = hProcess;

        if (hProcess == nint.Zero)
            return string.Empty;

        StringBuilder sb = new(1024);
        uint bufferSize = (uint)sb.Capacity;
        if (!Native.QueryFullProcessImageName(hProcess, 0, sb, ref bufferSize))
            return string.Empty;

        return sb.ToString();
    }

    public static nint GetWindowFromProcessId(int processId)
    {
        nint windowHandle = nint.Zero;

        Native.EnumWindows((hWnd, lParam) =>
        {
            Native.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == processId)
            {
                windowHandle = hWnd;
                return false;
            }

            return true;
        }, nint.Zero);

        return windowHandle;
    }

    public static unsafe List<nint> PatternScanAllOccurrences(nint module, string signature)
    {
        var (patternBytes, maskBytes) = ParseSignature(signature);

        var sizeOfImage = Native.GetModuleImageSize(module);
        var scanBytes = (byte*)module;

        if (Native.IsWine())
            Native.VirtualProtect(module, sizeOfImage, MemoryProtection.EXECUTE_READWRITE, out _);

        ReadOnlySpan<byte> span = new(scanBytes, (int)sizeOfImage);
        List<nint> offsets = [];

        long totalProcessed = 0L;
        while (true)
        {
            long offset = PatternScan(span, patternBytes, maskBytes);
            if (offset == -1)
                break;

            offsets.Add((nint)(module.ToInt64() + offset + totalProcessed));

            long processedOffset = offset + patternBytes.Length;
            totalProcessed += processedOffset;

            span = span.Slice((int)processedOffset);
        }

        return offsets;
    }

    public static long PatternScan(ReadOnlySpan<byte> data, byte[] patternBytes, bool[] maskBytes)
    {
        int s = patternBytes.Length;
        byte[] d = patternBytes;

        for (int i = 0; i < data.Length - s; i++)
        {
            bool found = true;
            for (int j = 0; j < s; j++)
            {
                if (d[j] != data[i + j] && !maskBytes[j])
                {
                    found = false;
                    break;
                }
            }

            if (found)
                return i;
        }

        return -1;
    }

    private static (byte[], bool[]) ParseSignature(string signature)
    {
        string[] tokens = signature.Split(' ');
        byte[]? patternBytes = [.. tokens.Select(x => x == "?" ? (byte)0xFF : Convert.ToByte(x, 16))];
        bool[] maskBytes = [.. tokens.Select(x => x == "?")];

        return (patternBytes, maskBytes);
    }

    public static nint GetModuleBase(nint hProcess, string moduleName)
    {
        string moduleNameLower = moduleName.ToLowerInvariant();
        nint[] modules = new nint[1024];

        if (!Native.EnumProcessModulesEx(hProcess, modules, (uint)(modules.Length * nint.Size), out uint bytesNeeded, 2))
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode != 299)
            {
                Debug.WriteLine($@"EnumProcessModulesEx failed ({errorCode}){Environment.NewLine}{Marshal.GetLastPInvokeErrorMessage()}"
                    , @"Error");
                return nint.Zero;
            }
        }

        foreach (nint module in modules.Where(x => x != nint.Zero))
        {
            StringBuilder sb = new(1024);
            if (Native.GetModuleBaseName(hProcess, module, sb, (uint)sb.Capacity) == 0)
                continue;

            if (sb.ToString().ToLowerInvariant() != moduleNameLower)
                continue;

            if (!Native.GetModuleInformation(hProcess, module, out var moduleInfo, (uint)Marshal.SizeOf<MODULEINFO>()))
                continue;

            return moduleInfo.lpBaseOfDll;
        }

        return nint.Zero;
    }
}
