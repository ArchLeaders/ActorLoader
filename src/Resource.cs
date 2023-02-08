using Nintendo.Byml;
using System.Text.Json;
using Yaz0Library;

namespace ActorLoader;

public class Resource
{
    public static Dictionary<string, JsonElement> BcmlSettings { get; } = GetBcmlSettings();

    private static Dictionary<string, JsonElement> GetBcmlSettings()
    {
        string bcmlSettingsJson = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "bcml", "settings.json");
        if (!File.Exists(bcmlSettingsJson)) {
            throw new FileNotFoundException("Could not find BCML settings, please make sure BCML is installed and setup before proceeding.");
        }

        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(bcmlSettingsJson))!;
    }

    public static string[] GetIgnoredList()
    {
        string ignoreListJson = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? string.Empty, "Data", "Ignored.json");
        if (File.Exists(ignoreListJson)) {
            return JsonSerializer.Deserialize<string[]>(File.ReadAllText(ignoreListJson))!;
        }

        return Array.Empty<string>();
    }

    public static BymlFile GetVanillaMapEntry(string unit, string map, string type)
    {
        string path = Path.Combine(BcmlSettings["dlc_dir"].GetString()!, "Map", map, unit, $"{unit}_{type}.smubin");
        return new(Yaz0.Decompress(path).ToArray());
    }

    public static BymlFile GetSrcActorInfo()
    {
        Stream stream = typeof(Program).Assembly.GetManifestResourceStream("ActorLoader.Data.ActorInfo.sbyml")!;
        Span<byte> data = stackalloc byte[(int)stream.Length]; // okay to stackalloc here because the filesize is const (195KB)
        stream.Read(data);

        byte[] decompressed = Yaz0.Decompress(data).ToArray();
        return new(decompressed);
    }

    public static BymlFile GetModActorInfo(string actorInfoPath)
    {
        if (!File.Exists(actorInfoPath)) {
            File.Copy(
                Path.Combine(BcmlSettings["update_dir"].GetString()!, "Actor", "ActorInfo.product.sbyml"),
                actorInfoPath);
        }

        return new(Yaz0.Decompress(actorInfoPath).ToArray());
    }

    public const string ActorBaseUrl = "https://github.com/ArchLeaders/ActorLoader/blob/master/src/Data/Actors/";
    public static async Task DownloadCActor(string path, string name)
    {
        Console.WriteLine($"  -> [{DateTime.Now:u}] [Downloading] | '{name}'");

        using FileStream fs = File.Create(path);
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("actor-loader", typeof(Program).Assembly.GetName().Version!.ToString());

        string url = $"{ActorBaseUrl}/{name}.sbactorpack";
        using Stream stream = await client.GetStreamAsync(url);
        stream.CopyTo(fs);
    }
}
