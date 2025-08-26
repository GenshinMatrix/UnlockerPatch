using System.Runtime.InteropServices;

namespace UnlockerPatch;

public static class UnlockerLauncher
{
    private static IntPtr MutexHandle = IntPtr.Zero;
    private static IntPtr hWnd = Native.GetConsoleWindow();
    private static CancellationTokenSource tokenSource = null!;
    public static string CommandLine = "";

    public static void showwindow(int status)
    {
        Native.ShowWindow(hWnd, status);
    }

    public static void Launch(string[] args)
    {
        MutexHandle = Native.CreateMutex(IntPtr.Zero, true, @"fpsunlocker");
        if (Marshal.GetLastWin32Error() == 183)
        {
            showwindow(5);
            Console.WriteLine(@"[Error]：Another fpsunlocker is already running.");
            return;
        }
        /*
         * SW_HIDE = 0;
         * SW_SHOW = 5;
         */
        showwindow(0);
        if (args.Length > 0)
        {
            for (int i = 0; i < args.Length; i++)
                CommandLine += args[i] + " ";
        }
        var configService = new ConfigService();
        var ipcService = new IpcService();
        // 创建 ProcessService 实例
        var processService = new ProcessService(configService, ipcService);
        tokenSource = new();
        if (processService.Start())
            while (!tokenSource.Token.IsCancellationRequested)
                Thread.Sleep(1000);
    }

    public static void Exit()
    {
        if (MutexHandle != IntPtr.Zero)
        {
            Native.ReleaseMutex(MutexHandle);
            Native.CloseHandle(MutexHandle);
            MutexHandle = IntPtr.Zero;
        }
        tokenSource.Cancel();
        showwindow(5);
    }
}
