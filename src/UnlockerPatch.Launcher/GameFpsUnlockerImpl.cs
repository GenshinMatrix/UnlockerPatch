using System.Diagnostics;

namespace UnlockerPatch;

internal sealed partial class GameFpsUnlockerImpl
{
    public static void Start(GenshinUnlockerOption option, string? gamePath = null, uint? pid = null, CancellationTokenSource? cts = null)
    {
        if (!option.UnlockFps.HasValue)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(gamePath) && pid == null)
        {
            return;
        }

        int targetPid = (int)pid!.Value;
        int targetFps = option.UnlockFps.Value;
        int ret = UnlockerLauncher.Unlock(targetPid, targetFps);

        Debug.WriteLine("[Unlocker] Unlock ret is " + ret);
    }
}
