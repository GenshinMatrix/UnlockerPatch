namespace UnlockerPatch.Launcher;

public class Program
{
    static async Task Main()
    {
        await UnlockerLauncher.StartAsync(@"D:\Program Files\Genshin Impact\Genshin Impact Game\YuanShen.exe", 120);
    }
}
