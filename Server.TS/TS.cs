using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.IO.Compression;

namespace SMan;

public class TS
{
    #region Interface
    public static void Update()
    {
        if (OperatingSystem.IsWindows())
        {
            var Latest = GetLatest();
            if (double.Parse(Latest) > GetCurrent())
            {
                try
                {
                    Runner.Kill();
                }
                catch (InvalidOperationException) { }
                MemoryStream Stream = new();
                new HttpClient().GetAsync(URL + Latest + "/teamspeak3-server_win64-" + Latest + ".zip").Result.Content.CopyTo(Stream, null, new CancellationTokenSource().Token);
                var Temp = Path.GetTempFileName();
                File.Delete(Temp);
                new ZipArchive(Stream).ExtractToDirectory(Temp);
                FileSystem.MoveDirectory(Temp, CWD, true);
            }
        }
    }

    public static void Run()
    {
        try
        {
            if (Runner.HasExited)
                Runner.Start();
        }
        catch (InvalidOperationException)
        {
            Runner.Start();
        }
    }

    public static void Clean()
    {
        if (Directory.Exists(Logs))
            Task.Run(() => new DirectoryInfo(Logs).GetFiles().ToList().ForEach(File =>
            {
                try
                { File.Delete(); }
                catch (IOException) { }
            }));
    }

    static string GetLatest() => new string(new HttpClient().GetAsync(URL).Result.Content.ReadAsStringAsync().Result.ToCharArray().Where(Char => !char.IsWhiteSpace(Char)).ToArray()).Split("<ahref=\"").Select(Part => Part.Split('"')[0]).Where(Release => double.TryParse(Release, out var Scrap)).MaxBy(Release => double.Parse(Release)) ?? throw new NotImplementedException();

    static double GetCurrent() => File.Exists(Changelog) ? File.ReadAllLines(Changelog).Where(Line => Line.StartsWith(Header)).Select(Line => Line.Replace(Header, string.Empty).Split(' ')[1]).Select(Line => double.Parse(Line.Replace(Header, string.Empty).Split(' ')[0])).Max() : 0;
    #endregion
    #region Constants
    static readonly string CWD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!);
    const string URL = "https://files.teamspeak-services.com/releases/server/";
    static readonly string SubDir = Path.Combine(CWD, "teamspeak3-server_win64");
    static readonly string Logs = Path.Combine(SubDir, "logs");
    static readonly string Changelog = Path.Combine(SubDir, "changelog.txt");
    static readonly string Binary = Path.Combine(SubDir, "ts3server.exe");
    const string Header = "## Server Release";
    #endregion
    #region Plumbing
    static readonly Process Runner = new() { StartInfo = new(Binary) { WorkingDirectory = SubDir } };
    #endregion
}