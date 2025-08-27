namespace UnlockerPatch;

public static class UnlockerLauncher
{
    public static string GamePath = null!;
    public static string CommandLine = string.Empty;
    public static int TargetFps = 144;

    public static bool Start(string gamePath, int targetFps, string? cli = null)
    {
        GamePath = gamePath ?? throw new ArgumentNullException(nameof(gamePath));
        TargetFps = targetFps;
        CommandLine = cli ?? string.Empty;

        ProcessService processService = new();
        return processService.Start();
    }
}
