namespace UnlockerPatch;

public static class UnlockerLauncher
{
    public static string GamePath = null!;
    public static string CommandLine = string.Empty;
    public static int TargetFps = 144;
    public static CancellationToken Token = default;

    public static async Task StartAsync(string gamePath, int targetFps, string? cli = null, CancellationToken token = default)
    {
        GamePath = gamePath ?? throw new ArgumentNullException(nameof(gamePath));
        TargetFps = targetFps;
        CommandLine = cli ?? string.Empty;
        Token = token;

        ProcessService processService = new();
        if (processService.Start())
            while (!token.IsCancellationRequested)
                await Task.Delay(2000);
    }
}
