﻿using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;

namespace SMan;

public class KF2
{
    #region Configuration
    public bool? UsedForTakeover;
    public int? Offset, OffsetWebAdmin;
    public string? ServerName, GamePassword, AdminPassword, BannerLink, WebsiteLink, ConfigSubDir;
    public IEnumerable<string>? ServerMOTD;
    public Games? Game;
    public Difficultues? Difficulty;
    public GameLengths? GameLength;
    #endregion
    #region Interface
    //https://wiki.killingfloor2.com/index.php?title=Dedicated_Server_(Killing_Floor_2)
    public static string[] Example => new[] { "KF-BurningParis", "KF-Bioticslab", "KF-Outpost", "KF-VolterManor", "KF-Catacombs", "KF-EvacuationPoint" };

    public static IEnumerable<ulong> GetIDs(ulong ID)
    {
        const string URLWorkshop = "https://steamcommunity.com/sharedfiles/filedetails/?id=";
        const string Header = $"<divclass=\"workshopItem\"><ahref=\"{URLWorkshop}";
        const string Footer = "\"";
        var Page = new string(new HttpClient().GetAsync($"{URLWorkshop}{ID}").Result.Content.ReadAsStringAsync().Result.ToCharArray().Where(Char => !char.IsWhiteSpace(Char)).ToArray());
        var Result = Enumerable.Empty<ulong>();
        var Next = 0;
        while (true)
        {
            var Current = Page.IndexOf(Header, Next, StringComparison.InvariantCultureIgnoreCase) + Header.Length;
            if (-1 != Current)
            {
                Next = Page.IndexOf(Footer, Current, StringComparison.InvariantCultureIgnoreCase);
                if (ulong.TryParse(Page[Current..Next], out var Scrap))
                    Result = Result.Append(Scrap);
                else break;
            }
            else
                break;
        }
        Result = Result.Order();
        return Result;
    }

    public static (IEnumerable<string>, IEnumerable<string>) GetMaps(IEnumerable<string>? Stock = null, IEnumerable<ulong>? IDs = null)
    {
        Stock ??= Enumerable.Empty<string>();
        if (IDs is not null)
        {
            if (Directory.Exists(Cache))
                Clean(Cache, IDs);
            else
                Directory.CreateDirectory(Cache);
            Clean(Workshop, IDs);
        }
        while (true)
        {
            var Runner = new KF2() { GamePassword = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) };
            Runner.Run(IDs: IDs);
            Task.WaitAny(new[]
                {
                    Task.Run(() => Runner.Wait()),
                    Task.Run(() =>
                    {
                        while (IDs is not null && IDs.Where(ID => !Directory.EnumerateDirectories(Cache).Select(ID => ulong.Parse(ID.GetDirectoryName())).Contains(ID)).Any())
                        {
                            Thread.Sleep(new TimeSpan(0,1,0));
                        }
                    }),
                });
            if (Runner.Running)
            {
                Runner.Kill();
                break;
            }
        }
        var Missing = GetValue(Path.Combine(Config, KFGame), KFGameInfo, GameMapCycles).Split('"')[1..^1].Where(Map => ',' != Map[0]).Where(Map => !Stock.Contains(Map));
        if (Missing.Any())
            Stock = Stock.Concat(Missing.Shuffle()).ToList();
        return (Stock, Directory.EnumerateDirectories(Cache).OrderBy(ID => ulong.Parse(ID.GetDirectoryName())).Select(ID => Directory.EnumerateFiles(ID, "*.kfm", SearchOption.AllDirectories).Last()).Select(ID => Path.GetFileNameWithoutExtension(ID)));
    }

    public static void Update()
    {
        do
            RunSteamCMD(AppID);
        while (!File.Exists(KFServer));
    }

    public static void Clean()
    {
        Clean(Logs);
        Clean(Dumps);

        void Clean(string Folder)
        {
            if (Directory.Exists(Folder))
                Task.Run(() => new DirectoryInfo(Folder).GetFiles().ToList().ForEach(File =>
                {
                    try
                    { File.Delete(); }
                    catch (IOException) { }
                }));
        }
    }

    public static void Clean(string Cache, IEnumerable<ulong> IDs)
    {
        if (Directory.Exists(Cache))
            Task.Run(() =>
                Directory.EnumerateDirectories(Cache).ToList().ForEach(ID =>
                {
                    if (!IDs.Contains(ulong.Parse(ID.GetDirectoryName())))
                        Directory.Delete(ID, true);
                })
            );
    }

    public bool Running => !Runner.HasExited;

    public void Wait() => Runner.WaitForExit();

    public void Kill() => Runner.Kill();

    public void Run(IEnumerable<string>? Maps = null, IEnumerable<ulong>? IDs = null, string? Map = null)
    {
        ConfigSubDir ??= ServerName;
        Init(Maps, IDs);
        var Log = Path.ChangeExtension(Path.GetRandomFileName(), Extension);
        Runner.StartInfo = new(KFServer, Maps is null ? $"-log={Log}" : $"{Map ?? Maps!.Random()}{(ConfigSubDir is not null ? "?ConfigSubDir=\"" + ConfigSubDir + "\"" : string.Empty)}{(Game is not null && Games.Survival != Game ? "?Game=" + Game?.Decode() : string.Empty) }{(AdminPassword is not null ? "?AdminPassword=" + AdminPassword : string.Empty) }{(GamePassword is not null ? "?GamePassword=" + GamePassword : string.Empty) }{(GameLength is not null ? "?GameLength=" + (int)GameLength : string.Empty)}{(Difficulty is not null ? "?Difficulty=" + (int)Difficulty : string.Empty)}{(Offset is not null ? "?Port=" + (Base + Offset) : string.Empty)}{(OffsetWebAdmin is not null ? "?WebAdminPort=" + (AdminBase + OffsetWebAdmin) : string.Empty)} -log={Log} -autoupdate");
        Log = Path.Combine(Logs, Log);
        HackINIs();
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
            if (HackINIs())
                Runner.Kill();
            else
                break;
            Task.Run(() =>
            {
                Runner.WaitForExit();
                File.Delete(Log);
            });
        }
    }
    #endregion
    #region SteamCMD
    static void RunSteamCMD(int AppID, string? UserName = null)
    {
        if (!File.Exists(SteamCMD))
        {
            MemoryStream Stream = new();
            new HttpClient().GetAsync(URLSteamCMD).Result.Content.CopyTo(Stream, null, new CancellationTokenSource().Token);
            if (OperatingSystem.IsWindows())
                new ZipArchive(Stream).ExtractToDirectory(CWD);
            else
                throw new PlatformNotSupportedException();
        }
        Process.Start(SteamCMD, $"+login {UserName ?? "anonymous"} +app_update {AppID} +quit")!.WaitForExit();
    }
    #endregion
    #region KFServer

    void Init(IEnumerable<string>? Maps, IEnumerable<ulong>? IDs)
    {
        this.Maps = Maps?.ToArray();
        this.IDs = IDs;
        DirectoryConfig = Path.Combine(Config, ConfigSubDir ?? string.Empty);
        FileKFGame = Path.Combine(DirectoryConfig, KFGame);
        FileKFEngine = Path.Combine(DirectoryConfig, KFEngine);
        FileKFWeb = Path.Combine(DirectoryConfig, KFWeb);
        Sanitize(ref ServerName);
        Sanitize(ref GamePassword);
        Sanitize(ref AdminPassword);
        Sanitize(ref BannerLink);
        Sanitize(ref WebsiteLink);
        if (!ServerMOTD?.Any() ?? false)
            ServerMOTD = null;

        static void Sanitize(ref string? Setting)
        {
            if (string.Empty == Setting?.Trim())
                Setting = null;
        }
    }

    bool HackINIs()
    {
        if (!TryReadINIs())
            return true;
        HackedKFGame = Maps is not null ? (
            (AdminPassword is not null && TrySet(ContentKFGame!, "Engine.GameInfo", "bAdminCanPause", true)) |
            (ServerName is not null && TrySet(ContentKFGame!, "Engine.GameReplicationInfo", "ServerName", ServerName)) |
            (BannerLink is not null && TrySet(ContentKFGame!, KFGameInfo, "BannerLink", BannerLink)) |
            (ServerMOTD is not null && TrySet(ContentKFGame!, KFGameInfo, "ServerMOTD", string.Join("\\n", ServerMOTD))) |
            (WebsiteLink is not null && TrySet(ContentKFGame!, KFGameInfo, "WebsiteLink", WebsiteLink)) |
            TrySet(ContentKFGame!, KFGameInfo, "ClanMotto", string.Empty) |
            TrySet(ContentKFGame!, KFGameInfo, "bDisableTeamCollision", true) |
            TrySet(ContentKFGame!, KFGameInfo, GameMapCycles, Encode(Maps))|
            TrySet(ContentKFGame!, "Engine.AccessControl", "GamePassword", string.Empty)) :
            TrySet(ContentKFGame!, "Engine.AccessControl", "GamePassword", GamePassword ?? string.Empty);
        HackedKFEngine =
            (Maps is not null && UsedForTakeover is not null && TrySet(ContentKFEngine!, "Engine.GameEngine", "bUsedForTakeover", UsedForTakeover!.Value)) |
            (IDs is not null ?
            (TryPrepend(ref ContentKFEngine!, TcpNetDriver, DownloadManagers, SteamWorkshopDownload) |
            TrySet(ref ContentKFEngine!, KFWorkshopSteamworks, ServerSubscribedWorkshopItems, IDs.Select(ID => ID.ToString()))) :
            (TryRemove(ref ContentKFEngine!, TcpNetDriver, DownloadManagers, SteamWorkshopDownload) |
            TryRemove(ref ContentKFEngine!, KFWorkshopSteamworks))
            );
        HackedKFWeb = Maps is not null && TrySet(ContentKFWeb!, "IpDrv.WebServer", "bEnabled", AdminPassword is not null);
        if (HackedKFGame)
            File.WriteAllLines(FileKFGame!, ContentKFGame!, Encoding.ASCII);
        if (HackedKFEngine)
            File.WriteAllLines(FileKFEngine!, ContentKFEngine!, Encoding.ASCII);
        if (HackedKFWeb)
            File.WriteAllLines(FileKFWeb!, ContentKFWeb!, Encoding.ASCII);
        return HackedKFGame || HackedKFEngine || HackedKFWeb;
    }

    bool TryReadINIs() => TryRead(FileKFGame!, ref ContentKFGame) && TryRead(FileKFEngine!, ref ContentKFEngine) && TryRead(FileKFWeb!, ref ContentKFWeb);
    #endregion
    #region INI
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
        var Index = FindValues(Data, Section, Key)[0];
        if (Value != GetValue(Data, Index))
        {
            Data = Data[0..Index].Append(Key + Separator + Value).Concat(Data[Index..]).ToArray();
            return true;
        }
        return false;
    }

    static bool TrySet(ref string[] Data, string Section, string Key, IEnumerable<string> Values)
    {
        var Indices = FindValues(Data, Section, Key);
        var Index = FindSection(Data, Section);
        if (!Indices.Any())
        {
            if (Index is null)
            {
                Data = Data.Append($"[{Section}]").ToArray();
                Index = Data.Length - 1;
            }
            Append(ref Data, Index!.Value, Key, Values);
            return true;
        }
        else
        {
            var ROCopy = Data;
            if (Indices.Select(Index => GetValue(ROCopy, Index)).Order().SequenceEqual(Values.Order()))
                return false;
            else
            {
                Data = Data.Where((_, Index) => !Indices.Contains(Index)).ToArray();
                Append(ref Data, Index!.Value, Key, Values);
                return true;
            }
        }

        static void Append(ref string[] Data, int Index, string Key, IEnumerable<string> Values)
        {
            foreach (var Value in Values.Reverse())
                Data = Data[0..(Index + 1)].Append(Key + Separator + Value).Concat(Data[(Index + 1)..]).ToArray();
        }
    }

    static bool TryRemove(ref string[] Data, string Section)
    {
        var Index = FindSection(Data, Section);
        if (Index is not null)
        {
            while (Data.Length > Index && (Data[Index.Value].StartsWith($"[{Section}]") || !Data[Index.Value].StartsWith("[")))
                Data = Data[0..Index.Value].Concat(Data[(Index.Value + 1)..]).ToArray();
            return true;
        }
        return false;
    }

    static bool TryRemove(ref string[] Data, string Section, string Key, string Value)
    {
        foreach (var Index in FindValues(Data, Section, Key))
            if (Value == GetValue(Data, Index))
            {
                Data = Data[0..Index].Concat(Data[(Index + 1)..]).ToArray();
                return true;
            }
        return false;
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

    static string Encode(IEnumerable<string> Maps) => $"(Maps=(\"{string.Join("\",\"", Maps)}\"))";

    static string GetValue(string Config, string Section, string Key) => GetValue(File.ReadAllLines(Config), Section, Key);
    #endregion
    #region File system
    static string ReadAllText(string Path)
    {
        using FileStream Stream = new(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return new StreamReader(Stream).ReadToEnd();
    }

    static bool TryRead(string Path, ref string[]? Collection)
    {
        if (File.Exists(Path))
        {
            Collection = File.ReadAllLines(Path, Encoding.ASCII);
            return true;
        }
        else
            return false;
    }
    #endregion
    #region Plumbing
    string? FileKFGame, FileKFEngine, FileKFWeb, DirectoryConfig;
    bool HackedKFGame, HackedKFEngine, HackedKFWeb;
    string[]? ContentKFGame, ContentKFEngine, ContentKFWeb, Maps;
    IEnumerable<ulong>? IDs;
    readonly Process Runner = new();
    #endregion
    #region Constants   
    const int Base = 7777 + 1;
    const int AdminBase = 8080 + 1;
    const string KFGame = "PCServer-KFGame.ini";
    const string KFEngine = "PCServer-KFEngine.ini";
    const string KFWeb = "KFWeb.ini";
    const string KFGameInfo = "KFGame.KFGameInfo";
    const string GameMapCycles = "GameMapCycles";
    const string TcpNetDriver = "IpDrv.TcpNetDriver";
    const string DownloadManagers = "DownloadManagers";
    const string SteamWorkshopDownload = "OnlineSubsystemSteamworks.SteamWorkshopDownload";
    const string KFWorkshopSteamworks = "OnlineSubsystemSteamworks.KFWorkshopSteamworks";
    const string ServerSubscribedWorkshopItems = "ServerSubscribedWorkshopItems";
    const string InitCompleted = "Initializing Game Engine Completed";
    const string Extension = "log";
    const char Separator = '=';
    const int AppID = 232130;
    static readonly string CWD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!);
    static readonly string URLSteamCMD = Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip",
        PlatformID.Unix => "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz",
        _ => throw new PlatformNotSupportedException()
    };
    static readonly string SteamCMD = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "steamcmd.exe",
        PlatformID.Unix => "steamcmd.sh",
        _ => throw new PlatformNotSupportedException()
    });
    static readonly string KFServer = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\Binaries\Win64\KFServer.exe",
        _ => throw new PlatformNotSupportedException()
    });
    static readonly string Logs = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\KFGame\Logs",
        _ => throw new PlatformNotSupportedException()
    });
    static readonly string Cache = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\KFGame\Cache",
        _ => throw new PlatformNotSupportedException()
    });
    static readonly string Config = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\KFGame\Config",
        _ => throw new PlatformNotSupportedException()
    });
    static readonly string Workshop = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\Binaries\Win64\steamapps\workshop\content\232090",
        _ => throw new PlatformNotSupportedException()
    });
    static readonly string Dumps = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "dumps",
        _ => throw new PlatformNotSupportedException()
    });
    #endregion
    #region Types
    public enum Games
    {
        Survival = 0,
        [EnumMember(Value = "KFGameContent.KFGameInfo_WeeklySurvival")]
        WeeklySurvival = 2,
        [EnumMember(Value = "KFGameContent.KFGameInfo_Endless")]
        Endless = 3,
    };
    public enum Difficultues
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
    public enum GameLengths
    {
        Short = 0,
        Normal = 1,
        Long = 2,
    }
    #endregion
}

public static class ExtensionMethods
{
    public static IEnumerable<T> Order<T>(this IEnumerable<T> Collection) => Collection.OrderBy(Item => Item);

    internal static IEnumerable<T> Shuffle<T>(this IEnumerable<T> Collection) => Collection.OrderBy(Item => PRNG.Next());

    internal static string GetDirectoryName(this string Directory) => Path.TrimEndingDirectorySeparator(Directory).Split(Path.DirectorySeparatorChar)[^1];

    internal static string Decode<T>(this T Enum) where T : Enum => typeof(T).GetMember(Enum!.ToString()!).Single().GetCustomAttributes(false).OfType<EnumMemberAttribute>().Single().Value!;

    internal static T Random<T>(this IEnumerable<T> Collection) => Collection.ElementAt(PRNG.Next(0, Collection.Count()));

    static readonly Random PRNG = new();
}