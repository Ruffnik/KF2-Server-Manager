using Microsoft.Extensions.Configuration;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;

namespace SMan;

public partial class Program
{
    #region Constants
    const string KFGame = "PCServer-KFGame.ini";
    const string KFEngine = "PCServer-KFEngine.ini";
    const string KFWeb = "KFWeb.ini";
    const string KFGameInfo = "KFGame.KFGameInfo";
    const string GameMapCycles = "GameMapCycles";
    const string InitCompleted = "Initializing Game Engine Completed";
    const string Ext = "log";
    const string Env = "Env";
    const char Separator = '=';
    static readonly string Self = System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!;
    static readonly string Files = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Self);
    static readonly string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Self);
    static readonly string URLSteamCMD = OperatingSystem.IsWindows() ?
        "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" :
        OperatingSystem.IsLinux() ?
        "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz" :
        throw new PlatformNotSupportedException();
    static readonly string SteamCMD = Path.Combine(Files,
        OperatingSystem.IsWindows() ?
        "steamcmd.exe" :
        OperatingSystem.IsLinux() ?
        "steamcmd.sh" :
        throw new PlatformNotSupportedException());
    static readonly string Server = Path.Combine(Files,
        OperatingSystem.IsWindows() ?
        @"steamapps\common\kf2server\Binaries\Win64\KFServer.exe" :
        throw new PlatformNotSupportedException());
    static readonly string Config = Path.Combine(Files,
        OperatingSystem.IsWindows() ?
        @"steamapps\common\kf2server\KFGame\Config" :
        throw new PlatformNotSupportedException());
    static readonly string Logs = Path.Combine(Files,
               OperatingSystem.IsWindows() ?
        @"steamapps\common\kf2server\KFGame\Logs" :
        throw new PlatformNotSupportedException());
    static readonly string Cache = Path.Combine(Files,
               OperatingSystem.IsWindows() ?
        @"steamapps\common\kf2server\KFGame\Cache" :
        throw new PlatformNotSupportedException());
    static readonly string Prefix =
        OperatingSystem.IsWindows() ?
        "/" :
        OperatingSystem.IsLinux() ?
        "--" :
        throw new PlatformNotSupportedException();
    static readonly string Help = $"\"{Self}\" [{Prefix}{Env} {{Files|Data}}]{Environment.NewLine}https://docs.microsoft.com/dotnet/core/extensions/configuration-providers#command-line-configuration-provider";
    const int AppID = 232130;
    enum Modes
    {
        Survival = 0,
        [EnumMember(Value = "KFGameContent.KFGameInfo_WeeklySurvival")]
        WeeklySurvival = 2,
        [EnumMember(Value = "KFGameContent.KFGameInfo_Endless")]
        Endless = 3,
    };
    enum Difficultues
    {
        [EnumMember(Value = "Normal")]
        Normal = 0,
        [EnumMember(Value = "Hard")]
        Hard = 1,
        [EnumMember(Value = "Suicidal")]
        Suicidal = 2,
        [EnumMember(Value = "Hell on Earth")]
        HellOnEarth = 3,
    }
    enum Lengths
    {
        Short = 0,
        Normal = 1,
        Long = 2,
    }
    #endregion
    #region Business logic
    public static void Main(string[] args)
    {
        if (!args.Any())
        {
            using Mutex Mutex = new(false, "Global\\{A21AFB32-FCB6-44C7-8C49-9729B3116FD2}");
            if (Mutex.WaitOne(0, false) && !Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Server)).Any())
            {
                Task.WaitAll(new Task[]
                {
                    Task.Run(() =>
                    {
                        if (Directory.Exists(Logs))
                            Directory.EnumerateFiles(Logs, $"*.{Ext}").ToList().ForEach(Log => File.Delete(Log));
                    }),
                    Task.Run(() =>
                    {
                        do
                        RunSteamCMD(AppID);
                        while (!File.Exists(Server));
                    }),
                });
                var Values = Enumerable.Empty<int>();
                foreach (var Difficulty in Enum.GetValues<Difficultues>())
                {
                    Values = Values.Append((int)Difficulty);
                    Console.WriteLine($"{(int)Difficulty}={Decode(Difficulty)}");
                }
                while (true)
                {
                    Console.Write($"{Settings.Default.Difficulty}>");
                    var Line = Console.ReadLine()!.Trim();
                    if (!Line.Any())
                        break;
                    else if (int.TryParse(Line, out var Difficulty) && Values.Contains(Difficulty))
                    {
                        Settings.Default.Difficulty = Difficulty;
                        Settings.Default.Save();
                        break;
                    }
                };
                RunServer(GetMaps());
            }
            else
                return;
        }
        else
            Console.WriteLine(new ConfigurationBuilder().AddCommandLine(args).Build()[Env] switch
            {
                nameof(Files) => Files,
                nameof(Data) => Data,
                _ => Help
            });
    }

    static Process RunServer(string[]? Maps = null, bool Flag = false)
    {
        Maps ??= Settings.Default.StockMaps.Cast<string>().ToArray();
        var Log = Path.ChangeExtension(Path.GetRandomFileName(), Ext);
        Process Runner = new() { StartInfo = new(Server, Flag ? $"{Maps!.ElementAt(PRNG.Next(0, Maps!.Length))}?GamePassword={Path.GetFileNameWithoutExtension(Path.GetRandomFileName())} -log={Log}" : $"{Maps!.ElementAt(PRNG.Next(0, Maps!.Length))}?ConfigSubDir=\"{Settings.Default.ServerName}\"{(Modes.Survival != (Modes)Settings.Default.Mode ? "?Game=" + Decode((Modes)Settings.Default.Mode) : string.Empty) }{(Settings.Default.AdminPassword.Any() ? "?AdminPassword=" + Settings.Default.AdminPassword : string.Empty) }{(Settings.Default.GamePassword.Any() ? "?GamePassword=" + Settings.Default.GamePassword : string.Empty) }?GameLength={(int)Lengths.Normal}?Difficulty={Settings.Default.Difficulty} -log={Log} -autoupdate") };
        Log = Path.Combine(Logs, Log);
        HackINIs(Flag ? Config : Path.Combine(Config, Settings.Default.ServerName), Maps, Flag);
        while (true)
        {
            if (File.Exists(Log))
                try
                {
                    File.Delete(Log);
                }
                catch (IOException)
                {
                    continue;
                }
            Runner.Start();
            while (!(File.Exists(Log) && 0 < new FileInfo(Log).Length && ReadAllText(Log).Contains(InitCompleted))) { }
            if (HackINIs(Flag ? Config : Path.Combine(Config, Settings.Default.ServerName), Maps, Flag))
                Runner.Kill();
            else
                break;
        }
        return Runner;
    }

    static ulong[] GetWorkshopIDs()
    {
        const string URLWorkshop = "https://steamcommunity.com/sharedfiles/filedetails/?id=";
        const string Header = $"<divclass=\"workshopItem\"><ahref=\"{URLWorkshop}";
        const string Footer = "\"";
        var Collection = new string(new HttpClient().GetAsync($"{URLWorkshop}{Settings.Default.Collection}").Result.Content.ReadAsStringAsync().Result.ToCharArray().Where(Char => !char.IsWhiteSpace(Char)).ToArray());
        var Result = Enumerable.Empty<ulong>();
        var Next = 0;
        while (true)
        {
            var Current = Collection.IndexOf(Header, Next, StringComparison.InvariantCultureIgnoreCase) + Header.Length;
            if (-1 != Current)
            {
                Next = Collection.IndexOf(Footer, Current, StringComparison.InvariantCultureIgnoreCase);
                if (ulong.TryParse(Collection[Current..Next], out var Scrap))
                    Result = Result.Append(Scrap);
                else break;
            }
            else
                break;
        }
        Result = Result.OrderBy(ID => ID);
        Settings.Default.WorkshopMaps = ToCollection(Result.Select(ID => ID.ToString()).ToArray());
        Settings.Default.Save();
        return Result.ToArray();
    }

    static string[] GetMaps()
    {
        var Result = Settings.Default.StockMaps.Cast<string>();
        var IDs = GetWorkshopIDs();
        if (Directory.Exists(Cache))
            Directory.EnumerateDirectories(Cache).ToList().ForEach(ID =>
            {
                if (!IDs.Contains(ulong.Parse(GetDirectoryName(ID))))
                    Directory.Delete(ID, true);
            });
        else
            Directory.CreateDirectory(Cache);
        while (true)
        {
            var Runner = RunServer(Flag: true);
            Task.WaitAny(new Task[]
                {
                    Task.Run(() => Runner.WaitForExit()),
                    Task.Run(() =>{while (IDs.Where(ID => !Directory.EnumerateDirectories(Cache).Select(ID => ulong.Parse(GetDirectoryName(ID))).Contains(ID)).Any()){Thread.Sleep(60000); }}),
                });
            if (!Runner.HasExited)
            {
                Runner.Kill();
                break;
            }
        }
        var Missing = GetValue(Path.Combine(Config, KFGame), KFGameInfo, GameMapCycles).Split('"')[1..^1].Where(Map => ',' != Map[0]).Where(Map => !Result.Contains(Map));
        if (Missing.Any())
        {
            Result = Result.Concat(Missing.OrderBy(Map => PRNG.Next()));
            Settings.Default.StockMaps = ToCollection(Result);
            Settings.Default.Save();
            File.WriteAllLinesAsync(Path.Combine(Data, Path.ChangeExtension(string.Join(string.Empty, DateOnly.FromDateTime(DateTime.Now).ToString("o").Split(Path.GetInvalidFileNameChars())), "cfg")), Result);
        }
        Result = Result.Concat(Directory.EnumerateDirectories(Cache).OrderBy(ID => ulong.Parse(GetDirectoryName(ID))).Select(ID => Directory.EnumerateFiles(ID, "*.kfm", SearchOption.AllDirectories).Single(ID => true)).Select(ID => Path.GetFileNameWithoutExtension(ID)));
        return Result.ToArray();
    }

    static bool HackINIs(string Config, string[] Maps, bool Flag = false) => (!Flag && HackKFGame(Path.Combine(Config, KFGame), Maps)) | HackKFEngine(Path.Combine(Config, KFEngine), Flag) | (!Flag && HackKFWeb(Path.Combine(Config, KFWeb)));

    static bool HackKFGame(string Config, string[] Maps)
    {
        if (!File.Exists(Config))
            return true;
        var Result = false;
        var Data = File.ReadAllLines(Config, Encoding.ASCII);
        Result |= TrySet(Data, "Engine.GameInfo", "bAdminCanPause", Settings.Default.AdminPassword.Any());
        Result |= TrySet(Data, "Engine.GameReplicationInfo", "ServerName", Settings.Default.ServerName);
        Result |= TrySet(Data, KFGameInfo, "BannerLink", Settings.Default.BannerLink);
        Result |= TrySet(Data, KFGameInfo, "ServerMOTD", Settings.Default.ServerMOTD);
        Result |= TrySet(Data, KFGameInfo, "WebsiteLink", Settings.Default.WebsiteLink);
        Result |= TrySet(Data, KFGameInfo, "bDisableTeamCollision", true);
        Result |= TrySet(Data, KFGameInfo, GameMapCycles, Encode(Maps));
        if (Result)
            File.WriteAllLines(Config, Data, Encoding.ASCII);
        return Result;
    }

    static bool HackKFEngine(string Config, bool Flag = false)
    {
        if (!File.Exists(Config))
            return true;
        var Result = false;
        var Data = File.ReadAllLines(Config, Encoding.ASCII);
        Result |= !Flag && TrySet(Data, "Engine.GameEngine", "bUsedForTakeover", Settings.Default.UsedForTakeover);
        Result |= TryPrepend(ref Data, "IpDrv.TcpNetDriver", "DownloadManagers", "OnlineSubsystemSteamworks.SteamWorkshopDownload");
        Result |= TrySet(ref Data, "OnlineSubsystemSteamworks.KFWorkshopSteamworks", "ServerSubscribedWorkshopItems", Settings.Default.WorkshopMaps.Cast<string>().ToArray());
        if (Result)
            File.WriteAllLines(Config, Data, Encoding.ASCII);
        return Result;
    }

    static bool HackKFWeb(string Config)
    {
        if (!File.Exists(Config))
            return true;
        var Result = false;
        var Data = File.ReadAllLines(Config, Encoding.ASCII);
        Result |= TrySet(Data, "IpDrv.WebServer", "bEnabled", Settings.Default.AdminPassword.Any());
        if (Result)
            File.WriteAllLines(Config, Data, Encoding.ASCII);
        return Result;
    }

    static void RunSteamCMD(string Arguments)
    {
        if (!File.Exists(SteamCMD))
        {
            MemoryStream Stream = new();
            new HttpClient().GetAsync(URLSteamCMD).Result.Content.CopyTo(Stream, null, new CancellationTokenSource().Token);
            if (OperatingSystem.IsWindows())
                new ZipArchive(Stream).ExtractToDirectory(Files);
            else
                throw new PlatformNotSupportedException();
        }
        Process.Start(SteamCMD, Arguments)!.WaitForExit();
    }

    static string Encode(string[] Maps) => $"(Maps=(\"{string.Join("\",\"", Maps)}\"))";
    #endregion
    #region Utilities
    static bool TrySet(string[] Data, string Section, string Key, string Value)
    {
        if (Value != GetValue(Data, Section, Key))
        {
            SetValue(Data, Section, Key, Value);
            return true;
        }
        else
            return false;
    }

    static bool TrySet(string[] Data, string Section, string Key, bool Value)
    {
        if (!bool.TryParse(GetValue(Data, Section, Key), out var Result) || Result != Value)
        {
            SetValue(Data, Section, Key, Value.ToString());
            return true;
        }
        else
            return false;
    }

    private static bool TryPrepend(ref string[] Data, string Section, string Key, string Value)
    {
        var Find = FindValues(Data, Section, Key)[0];
        if (Value != GetValue(Data, Find))
        {
            Data = Data[0..Find].Append(Key + Separator + Value).Concat(Data[Find..]).ToArray();
            return true;
        }
        return false;
    }

    static bool TrySet(ref string[] Data, string Section, string Key, string[] Values)
    {
        var Finds = FindValues(Data, Section, Key);
        var Find = FindSection(Data, Section);
        if (!Finds.Any())
        {
            if (Find is null)
            {
                Data = Data.Append($"[{Section}]").ToArray();
                Find = Data.Length - 1;
            }
            Append(ref Data, Find!.Value, Key, Values);
            return true;
        }
        else
        {
            var ROCopy = Data;
            if (Finds.Select(Find => GetValue(ROCopy, Find)).OrderBy(ID => ID).SequenceEqual(Values.OrderBy(ID => ID)))
                return false;
            else
            {
                Data = Data.Where((Line, Find) => !Finds.Contains(Find)).ToArray();
                Append(ref Data, Find!.Value, Key, Values);
                return true;
            }
        }

        static void Append(ref string[] Data, int Index, string Key, string[] Values)
        {
            foreach (var Value in Values.Reverse())
                Data = Data[0..(Index + 1)].Append(Key + Separator + Value).Concat(Data[(Index + 1)..]).ToArray();
        }
    }

    static string GetValue(string[] Config, string Section, string Key) => GetValue(Config, FindValue(Config, Section, Key));

    static string GetValue(string[] Config, int Index) => string.Join(Separator, Config[Index].Split(Separator)[1..]);

    static void SetValue(string[] Config, string Section, string Key, string Value) => Config[FindValue(Config, Section, Key)] = Key + Separator + Value;

    static int? FindSection(string[] Data, string Section)
    {
        var Result = 0;
        while ($"[{Section}]" != Data[Result])
            if (Result < Data.Length - 1)
                Result++;
            else
                return null;
        return Result;
    }

    static int FindValue(string[] Data, string Section, string Key)
    {
        var Result = 0;
        while ($"[{Section}]" != Data[Result])
            Result++;
        while (!Data[Result].StartsWith(Key))
            Result++;
        return Result;
    }

    static int[] FindValues(string[] Data, string Section, string Key)
    {
        var Result = Enumerable.Empty<int>();
        var Next = 0;
        while (Data.Length > Next && $"[{Section}]" != Data[Next])
            Next++;
        while (Data.Length - 1 > Next && !Data[Next + 1].StartsWith($"["))
        {
            Next++;
            if (Data[Next].StartsWith(Key))
                Result = Result.Append(Next);
        }
        return Result.ToArray();
    }

    static string ReadAllText(string Path)
    {
        using FileStream Stream = new(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return new StreamReader(Stream).ReadToEnd();
    }

    static string Decode<T>(T Enum) where T : Enum => typeof(T).GetMember(Enum!.ToString()!).Single().GetCustomAttributes(false).OfType<EnumMemberAttribute>().Single().Value!;

    static StringCollection ToCollection(IEnumerable<string> Array)
    {
        StringCollection Result = new();
        Result.AddRange(Array.ToArray());
        return Result;
    }

    static string GetDirectoryName(string Directory) => Path.TrimEndingDirectorySeparator(Directory).Split(Path.DirectorySeparatorChar)[^1];
    #endregion
    #region Plumbing
    static readonly Random PRNG = new();
    static Program()
    {
        if (!Directory.Exists(Files))
            Directory.CreateDirectory(Files);
        if (!Directory.Exists(Data))
            Directory.CreateDirectory(Data);
    }
    static string GetValue(string Config, string Section, string Key) => GetValue(File.ReadAllLines(Config), Section, Key);
    static void RunSteamCMD(int AppID, string? UserName = null) => RunSteamCMD($"+login {UserName ?? "anonymous"} +app_update {AppID} +quit");
    #endregion
}