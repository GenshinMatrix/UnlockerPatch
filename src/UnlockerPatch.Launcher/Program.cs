
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UnlockerPatch.Launcher;

public class Program
{
    static async Task Main(string[] args)
    {
        // 查找游戏进程（可根据实际进程名修改）
        var gameProcess = Process.GetProcessesByName("YuanShen").FirstOrDefault();
        if (gameProcess == null)
        {
            Console.WriteLine("未找到游戏进程！");
            return;
        }

        // 设置解锁参数
        var unlocker = new GenshinFpsUnlocker(gameProcess)
            .SetTargetFps(120); // 可自定义目标FPS

        var options = GenshinUnlockerOption.Default.Value;
        options.IsUnlockFps = true;

        // 执行解锁
        await unlocker.UnlockAsync(options);
    }
}
