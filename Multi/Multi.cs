using System.Collections.Specialized;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Xml;

namespace SMan;

public class Multi
{
    public static void Main()
    {
        Setup();
        Persist();
        Cleanup();

        //Run(@"C:\Users\Kiril\AppData\Roaming\KF2 Server Manager\Private.xml");
        //Run(@"C:\Users\Kiril\AppData\Roaming\KF2 Server Manager\Public.xml");
    }

    static void Run(string Config)
    {
        var Runner = Deserialize<KF2>(Config)!;
        Runner.Run(Maps.Item1!.Concat(Maps.Item2!), /*IDs,*/ ConfigSubDir: Path.GetFileNameWithoutExtension(Config));
    }

    static void Cleanup()
    {
        KF2.Clean();
        //if (IDs is not null)
        //    KF2.Clean(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "My Games", "KillingFloor2", "KFGame", "Cache"), IDs);
    }

    static void Persist()
    {
        bool DoIDs = true;
        bool DoMaps = true;
        try
        {
            DoIDs = !IDs?.SequenceEqual(Decode(Settings.Default.IDs)) ?? false;
        }
        catch (ArgumentNullException) { }
        try
        {
            DoMaps = !Maps.Item1!.SequenceEqual(Settings.Default.Maps.Cast<string>());
        }
        catch (ArgumentNullException) { }
        if (DoMaps)
        {
            Settings.Default.Maps = Encode(Maps.Item1!);
            Serialize(nameof(Settings.Default.Maps), Path.Combine(CWD, string.Join(string.Empty, DateOnly.FromDateTime(DateTime.Now).ToString("o").Split(Path.GetInvalidFileNameChars()))), Settings.Default.Maps);
        }
        if (DoIDs)
            Settings.Default.IDs = Encode(IDs!);
        try
        {
            Settings.Default.ID = Settings.Default.ID;
        }
        catch (NullReferenceException) { }
        Settings.Default.Save();
    }

    static void Setup()
    {
        try
        { IDs = KF2.GetIDs(Settings.Default.ID); }
        catch (NullReferenceException) { }
        KF2.Update();
        try
        { Maps.Item1 = Settings.Default.Maps.Cast<string>(); }
        catch (ArgumentNullException) { }
        Maps = KF2.GetMaps(Maps.Item1, IDs);
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

    static IEnumerable<ulong>? IDs;
    static (IEnumerable<string>?, IEnumerable<string>?) Maps;
    static readonly string CWD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!);
    const string XML = "xml";
    const string ZIP = "zip";
    static readonly IEnumerable<KF2> Farm = Enumerable.Empty<KF2>();

    static Multi()
    {
        var Files = Directory.EnumerateFiles(CWD, "*." + XML);
        foreach (var Config in Files)
        {
            var Instance = Deserialize<KF2>(Config)!;
        }
        Farm = Directory.EnumerateFiles(CWD, "*." + XML).Select(Config => Deserialize<KF2>(Config)!);
    }
}