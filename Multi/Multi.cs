using System.Collections.Specialized;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Xml;

namespace SMan;

public class Multi
{
    public static void Main()
    {
#if !DEBUG
        try
        {
#endif
        while (true)
        {
            if (Farm.All(Server => !Server.Running))
                KF2.Update();
            Update();
            if (NewIDs || NewMaps)
                Kill();
            Cleanup();
            Run();
            if (Directory.Exists(Path.GetDirectoryName(Settings.Default.HTML)))
                Task.Run(() => File.WriteAllText(Settings.Default.HTML, GetHTML()));
            Task.Run(() =>
            {
                TS.Update();
                TS.Run();
            });
            Task.WaitAny(new[]
            {
                Task.Delay(new TimeSpan(1,0,0)),
                Task.Run(()=>Farm.ToList().AsParallel().ForAll(Server => Server.Wait()))
            });
        }
#if !DEBUG
    }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            if (OperatingSystem.IsWindows())
                Console.ReadKey();
        }
#endif
    }

    static void Run() => Farm.Where(Server => !Server.Running).ToList().AsParallel().ForAll(Server => Server.Run(Maps.Item1!.Concat(Maps.Item2!), IDs));

    static void Kill() => Farm.Where(Server => Server.Running).ToList().AsParallel().ForAll(Server => Server.Kill());

    static void Cleanup()
    {
        KF2.Clean();
        //Actually that's client stuff
        //if (IDs is not null)
        //    KF2.Clean(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "My Games", "KillingFloor2", "KFGame", "Cache"), IDs);
    }

    static void Update()
    {
        try
        { IDs = KF2.GetIDs(Settings.Default.ID); }
        catch (NullReferenceException) { }
        try
        { Maps.Item1 = Settings.Default.Maps.Cast<string>(); }
        catch (ArgumentNullException) { }
        Maps = KF2.GetMaps(Maps.Item1, IDs);
        try
        {
            NewIDs = !IDs?.SequenceEqual(Decode(Settings.Default.IDs)) ?? false;
        }
        catch (ArgumentNullException)
        { NewIDs = true; }
        try
        {
            NewMaps = !Maps.Item1!.SequenceEqual(Settings.Default.Maps.Cast<string>());
        }
        catch (ArgumentNullException)
        { NewMaps = true; }
        if (NewMaps)
        {
            Settings.Default.Maps = Encode(Maps.Item1!);
            Serialize(nameof(Settings.Default.Maps), Path.Combine(CWD, string.Join(string.Empty, DateOnly.FromDateTime(DateTime.Now).ToString("o").Split(Path.GetInvalidFileNameChars()))), Settings.Default.Maps);
        }
        if (NewIDs)
            Settings.Default.IDs = Encode(IDs!);
        try
        {
            Settings.Default.ID = Settings.Default.ID;
        }
        catch (NullReferenceException) { }
        Settings.Default.Save();
    }

    static IEnumerable<ulong> Decode(StringCollection Collection) => Collection.Cast<string>().Select(Item => ulong.Parse(Item));

    static StringCollection Encode(IEnumerable<ulong> Collection) => Encode(Collection.Select(Item => Item.ToString()));

    static StringCollection Encode(IEnumerable<string> Collection)
    {
        StringCollection Result = new();
        Result.AddRange(Collection.ToArray());
        return Result;
    }

    static void Serialize(string FileName, string Container, object Data)
    {
        if (string.Empty == Path.GetExtension(FileName))
            FileName = Path.ChangeExtension(FileName, XML);
        if (string.Empty == Path.GetExtension(Container))
            Container = Path.ChangeExtension(Container, ZIP);
        using var Stream = new FileStream(Container, FileMode.Create);
        using var Archive = new ZipArchive(Stream, ZipArchiveMode.Create);
        var Entry = Archive.CreateEntry(FileName);
        using var Writer = XmlWriter.Create(Entry.Open());
        new DataContractSerializer(Data.GetType()).WriteObject(Writer, Data);
    }

    //Gonna need this one later for a possible editor
    //static void Serialize(string FileName, object Data)
    //{
    //    if (string.Empty == Path.GetExtension(FileName))
    //        FileName = Path.ChangeExtension(FileName, XML);
    //    using var Stream = new FileStream(FileName, FileMode.Create);
    //    using var Writer = XmlWriter.Create(Stream);
    //    new DataContractSerializer(Data.GetType()).WriteObject(Writer, Data);
    //}

    static T? Deserialize<T>(string FileName)
    {
        if (!File.Exists(FileName))
            FileName = Path.ChangeExtension(FileName, XML);
        var Stream = new FileStream(FileName, FileMode.Open);
        var Reader = XmlReader.Create(Stream);
        return (T?)new DataContractSerializer(typeof(T)).ReadObject(Reader);
    }

    static string GetHTML()
    {
        IP = new string(new HttpClient().GetAsync("http://www.what-is-my-ipv4.com").Result.Content.ReadAsStringAsync().Result.ToCharArray().Where(Char => !char.IsWhiteSpace(Char)).ToArray()).Split("<strongclass='ip'>")[1].Split("</strong>")[0];

        return $"<!doctype html><head>{GetHead()}</head><body>{GetBody()}</body></html>";

        static string GetHead()
        {
            var TheSource = $"http://{IP}:8081/images/";
            return $"<title>{Farm.Random().ServerName!}</title><link rel=\"shortcut icon\" href=\"{TheSource}favicon.ico\" type=\"image/x-icon\"><link rel=\"stylesheet\" type=\"text/css\" href=\"{TheSource}kf2.css\"><link rel=\"stylesheet\" type=\"text/css\" href=\"{TheSource}kf2modern.css\"><script type=\"text/javascript\">function WebAdmin(Port){{window.location.replace(window.location.protocol +\"//\"+window.location.hostname+\":\"+Port)}}</script>";
        }

        static string GetBody() => string.Join("<br>", Farm.Select(Server => (Server.Port, Server.ConfigSubDir, Server.PortWebAdmin)).Select(Server => $"{(Server.PortWebAdmin is not null ? $"<a href=# onclick=\"WebAdmin(" + Server.PortWebAdmin + ")\">&#x1f9d9</a>" : "&#x274c")}&nbsp;<a href=\"steam://rungameid/232090//-SteamConnectIP={IP}:{Server.Port}\">{Server.ConfigSubDir}</a>")) + "<footer>" + DateTime.Now.ToString("o") + "</footer>";
    }

    static Multi()
    {
        if (Directory.Exists(CWD))
            Directory.EnumerateFiles(CWD, "*." + XML).ToList().ForEach(Config =>
            {
                var Server = Deserialize<KF2>(Config)!;
                Server.ConfigSubDir = Path.GetFileNameWithoutExtension(Config);
                Server.Offset = Farm.Count();
                if (Server.AdminPassword is not null)
                    Server.OffsetWebAdmin = Farm.Where(Server => Server.AdminPassword is not null).Count();
                Farm = Farm.Append(Server);
            });
        else
            Directory.CreateDirectory(CWD);
    }

    static IEnumerable<ulong>? IDs;
    static (IEnumerable<string>?, IEnumerable<string>?) Maps;
    static readonly string CWD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!);
    const string XML = "xml";
    const string ZIP = "zip";
    static IEnumerable<KF2> Farm = Enumerable.Empty<KF2>();
    static bool NewIDs, NewMaps;
    static string IP = string.Empty;
}