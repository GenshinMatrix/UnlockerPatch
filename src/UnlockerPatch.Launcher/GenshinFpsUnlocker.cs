using System.Diagnostics;

namespace UnlockerPatch;

internal sealed class GenshinFpsUnlocker
{
    private readonly Process gameProcess;
    private int unlockFps;

    public GenshinFpsUnlocker(Process gameProcess)
    {
        this.gameProcess = gameProcess;
    }

    public GenshinFpsUnlocker SetTargetFps(int unlockFps)
    {
        this.unlockFps = unlockFps;
        return this;
    }

    public async Task UnlockAsync(GenshinUnlockerOption options, CancellationTokenSource cts = null)
    {
        options.UnlockFps = unlockFps;
        await Task.Run(() => GameFpsUnlockerImpl.Start(options, pid: (uint)gameProcess.Id, cts: cts));
    }
}
