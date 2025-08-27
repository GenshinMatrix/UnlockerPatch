namespace UnlockerPatch.Launcher;

public class Program
{
    static void Main()
    {
        UnlockerLauncher.Start(@"D:\Program Files\Genshin Impact\Genshin Impact Game\YuanShen.exe", 120);

        while (true)
            Thread.Sleep(2000);
    }
}
