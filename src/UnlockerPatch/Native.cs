using System.Runtime.InteropServices;
using System.Text;

namespace UnlockerPatch;

internal static class Native
{
    public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    public delegate void WinEventProc(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool TerminateProcess(nint hProcess, uint uExitCode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(nint hWinEventHook);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern nint SetWindowsHookEx(int idHook, nint lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    public static extern bool PostThreadMessage(uint idThread, uint Msg, nint wParam, nint lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(nint hHandle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool QueryFullProcessImageName(nint hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

    [DllImport("kernel32.dll")]
    public static extern bool GetExitCodeProcess(nint hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, nint lpProcessAttributes, nint lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, nint lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFreeEx(nint hProcess, nint lpAddress, uint dwSize, uint dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualProtect(nint lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint LoadLibraryEx(string lpFileName, nint hFile, uint dwFlags);

    [DllImport("kernel32.dll")]
    public static extern void FreeLibrary(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll")]
    public static extern nint GetProcAddress(nint hModule, string procedureName);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool EnumProcessModulesEx(nint hProcess, [Out] nint[] lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

    [DllImport("psapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint GetModuleBaseName(nint hProcess, nint hModule, StringBuilder lpBaseName, uint nSize);

    [DllImport("psapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool GetModuleInformation(nint hProcess, nint hModule, out MODULEINFO lpmodinfo, uint cb);

    public static bool IsWine()
    {
        var ntdll = GetModuleHandle("ntdll.dll");
        var ver = GetProcAddress(ntdll, "wine_get_version");

        return ver != 0;
    }

    public static uint GetModuleImageSize(nint lpBaseAddress)
    {
        var dosHeader = Marshal.PtrToStructure<IMAGE_DOS_HEADER>(lpBaseAddress);
        var ntHeader = Marshal.PtrToStructure<IMAGE_NT_HEADERS>(lpBaseAddress + dosHeader.e_lfanew);

        return ntHeader.OptionalHeader.SizeOfImage;
    }
}

internal class ModuleGuard(nint module) : IDisposable
{
    public nint BaseAddress { get => module & ~3; }

    public static implicit operator ModuleGuard(nint module) => new(module);

    public static implicit operator nint(ModuleGuard guard) => guard.BaseAddress;

    public static implicit operator bool(ModuleGuard guard) => guard.BaseAddress != nint.Zero;

    public void Dispose()
    {
        if (this)
            Native.FreeLibrary(module);
    }
}

internal static class ProcessAccess
{
    public const uint TERMINATE = 0x0001;
    public const uint CREATE_THREAD = 0x0002;
    public const uint SET_SESSIONID = 0x0004;
    public const uint VM_OPERATION = 0x0008;
    public const uint VM_READ = 0x0010;
    public const uint VM_WRITE = 0x0020;
    public const uint DUP_HANDLE = 0x0040;
    public const uint CREATE_PROCESS = 0x0080;
    public const uint SET_QUOTA = 0x0100;
    public const uint SET_INFORMATION = 0x0200;
    public const uint QUERY_INFORMATION = 0x0400;
    public const uint SUSPEND_RESUME = 0x0800;
    public const uint QUERY_LIMITED_INFORMATION = 0x1000;
    public const uint SET_LIMITED_INFORMATION = 0x2000;
    public const uint ALL_ACCESS = 0x1FFFFF;
}

internal static class StandardAccess
{
    public const uint DELETE = 0x00010000;
    public const uint READ_CONTROL = 0x00020000;
    public const uint WRITE_DAC = 0x00040000;
    public const uint WRITE_OWNER = 0x00080000;
    public const uint SYNCHRONIZE = 0x00100000;
    public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
    public const uint STANDARD_RIGHTS_READ = READ_CONTROL;
    public const uint STANDARD_RIGHTS_WRITE = READ_CONTROL;
    public const uint STANDARD_RIGHTS_EXECUTE = READ_CONTROL;
    public const uint STANDARD_RIGHTS_ALL = 0x001F0000;
    public const uint SPECIFIC_RIGHTS_ALL = 0x0000FFFF;
}

internal static class AllocationType
{
    public const uint COMMIT = 0x1000;
    public const uint RESERVE = 0x2000;
    public const uint RESET = 0x80000;
    public const uint LARGE_PAGES = 0x20000000;
    public const uint PHYSICAL = 0x400000;
    public const uint TOP_DOWN = 0x100000;
    public const uint WRITE_WATCH = 0x200000;
    public const uint RESET_UNDO = 0x1000000;
}

internal static class FreeType
{
    public const uint DECOMMIT = 0x4000;
    public const uint RELEASE = 0x8000;
}

internal static class MemoryProtection
{
    public const uint EXECUTE = 0x10;
    public const uint EXECUTE_READ = 0x20;
    public const uint EXECUTE_READWRITE = 0x40;
    public const uint EXECUTE_WRITECOPY = 0x80;
    public const uint NOACCESS = 0x01;
    public const uint READONLY = 0x02;
    public const uint READWRITE = 0x04;
    public const uint WRITECOPY = 0x08;
}

[StructLayout(LayoutKind.Sequential)]
public struct PROCESS_INFORMATION
{
    public nint hProcess;
    public nint hThread;
    public int dwProcessId;
    public int dwThreadId;
}

[StructLayout(LayoutKind.Sequential)]
public struct STARTUPINFO
{
    public int cb;
    public string lpReserved;
    public string lpDesktop;
    public string lpTitle;
    public int dwX;
    public int dwY;
    public int dwXSize;
    public int dwYSize;
    public int dwXCountChars;
    public int dwYCountChars;
    public int dwFillAttribute;
    public int dwFlags;
    public short wShowWindow;
    public short cbReserved2;
    public nint lpReserved2;
    public nint hStdInput;
    public nint hStdOutput;
    public nint hStdError;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct IMAGE_DOS_HEADER
{
    public ushort e_magic;          // Magic number
    public ushort e_cblp;           // Bytes on last page of file
    public ushort e_cp;             // Pages in file
    public ushort e_crlc;           // Relocations
    public ushort e_cparhdr;        // Size of header in paragraphs
    public ushort e_minalloc;       // Minimum extra paragraphs needed
    public ushort e_maxalloc;       // Maximum extra paragraphs needed
    public ushort e_ss;             // Initial (relative) SS value
    public ushort e_sp;             // Initial SP value
    public ushort e_csum;           // Checksum
    public ushort e_ip;             // Initial IP value
    public ushort e_cs;             // Initial (relative) CS value
    public ushort e_lfarlc;         // File address of relocation table
    public ushort e_ovno;           // Overlay number
    public fixed ushort e_res[4];   // Reserved words
    public ushort e_oemid;          // OEM identifier (for e_oeminfo)
    public ushort e_oeminfo;        // OEM information; e_oemid specific
    public fixed ushort e_res2[10]; // Reserved words
    public int e_lfanew;            // File address of new exe header
}

[StructLayout(LayoutKind.Sequential)]
public struct IMAGE_NT_HEADERS
{
    public uint Signature;
    public IMAGE_FILE_HEADER FileHeader;
    public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
}

[StructLayout(LayoutKind.Sequential)]
public struct IMAGE_FILE_HEADER
{
    public ushort Machine;
    public ushort NumberOfSections;
    public uint TimeDateStamp;
    public uint PointerToSymbolTable;
    public uint NumberOfSymbols;
    public ushort SizeOfOptionalHeader;
    public ushort Characteristics;
}

[StructLayout(LayoutKind.Sequential)]
public struct IMAGE_OPTIONAL_HEADER64
{
    // Standard fields.
    public ushort Magic;

    public byte MajorLinkerVersion;
    public byte MinorLinkerVersion;
    public uint SizeOfCode;
    public uint SizeOfInitializedData;
    public uint SizeOfUninitializedData;
    public uint AddressOfEntryPoint;
    public uint BaseOfCode;

    // Specific to IMAGE_OPTIONAL_HEADER64
    public ulong ImageBase;

    public uint SectionAlignment;
    public uint FileAlignment;
    public ushort MajorOperatingSystemVersion;
    public ushort MinorOperatingSystemVersion;
    public ushort MajorImageVersion;
    public ushort MinorImageVersion;
    public ushort MajorSubsystemVersion;
    public ushort MinorSubsystemVersion;
    public uint Win32VersionValue;
    public uint SizeOfImage;
    public uint SizeOfHeaders;
    public uint CheckSum;
    public ushort Subsystem;
    public ushort DllCharacteristics;
    public ulong SizeOfStackReserve;
    public ulong SizeOfStackCommit;
    public ulong SizeOfHeapReserve;
    public ulong SizeOfHeapCommit;
    public uint LoaderFlags;
    public uint NumberOfRvaAndSizes;

    // Directory Entries
    public IMAGE_DATA_DIRECTORY ExportTable;

    public IMAGE_DATA_DIRECTORY ImportTable;
    public IMAGE_DATA_DIRECTORY ResourceTable;
    public IMAGE_DATA_DIRECTORY ExceptionTable;
    public IMAGE_DATA_DIRECTORY CertificateTable;
    public IMAGE_DATA_DIRECTORY BaseRelocationTable;
    public IMAGE_DATA_DIRECTORY Debug;
    public IMAGE_DATA_DIRECTORY Architecture;
    public IMAGE_DATA_DIRECTORY GlobalPtr;
    public IMAGE_DATA_DIRECTORY TLSTable;
    public IMAGE_DATA_DIRECTORY LoadConfigTable;
    public IMAGE_DATA_DIRECTORY BoundImport;
    public IMAGE_DATA_DIRECTORY IAT;
    public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
    public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
    public IMAGE_DATA_DIRECTORY Reserved;
}

[StructLayout(LayoutKind.Sequential)]
public struct IMAGE_DATA_DIRECTORY
{
    public uint VirtualAddress;
    public uint Size;
}

[StructLayout(LayoutKind.Sequential)]
public struct MODULEINFO
{
    public nint lpBaseOfDll;
    public uint SizeOfImage;
    public nint EntryPoint;
}
