using System.Diagnostics;
using System.IO.Compression;

namespace SMan;

public class Program
{
    static readonly string Name = System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!;
    static readonly string CWD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name);
    static readonly string URL = OperatingSystem.IsWindows() ?
        "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" :
        OperatingSystem.IsLinux() ?
        "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz" :
        throw new PlatformNotSupportedException();
    static readonly string SteamCMD =
        OperatingSystem.IsWindows() ?
        "steamcmd.exe" :
        OperatingSystem.IsLinux() ?
        "steamcmd.sh" :
        throw new PlatformNotSupportedException();

    static readonly object Lock = new();

    public static void Main()
    {
        SCHost("+login anonymous +app_update 232130 +quit");
    }

    static void SCHost(string Arguments)
    {
        var Downloaded = false;
        var Executed = false;
        lock (Lock)
        {
            if (!Downloaded)
            {
                if (!File.Exists(Path.Combine(CWD, SteamCMD)))
                {
                    using var Stream = new MemoryStream();
                    new HttpClient().GetAsync(URL).Result.Content.CopyTo(Stream, null, new CancellationTokenSource().Token);
                    new ZipArchive(Stream).ExtractToDirectory(CWD);
                }
                Downloaded = true;
            }
            if (!Executed)
            {
                if (Arguments is null)
                    Process.Start(new ProcessStartInfo(SteamCMD, Arguments) { WorkingDirectory = CWD })!.WaitForExit();
                Executed = true;
            }
        }
    }

    static Program()
    {
        if (!Directory.Exists(CWD))
            Directory.CreateDirectory(CWD);
        Environment.CurrentDirectory = CWD;
    }
}