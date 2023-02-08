using Nintendo.Byml;
using System.Text.Json;
using Yaz0Library;

namespace ActorLoader;

public class ModFolder
{
	private readonly string _path;
	private readonly bool _auto; // not implemented yet

	private readonly string _modActorsPath;
	private readonly List<string> _modActors;

	private readonly string _cActorsPath;
	private readonly string[] _cActors;

	private readonly string[] _ignoredActors = Array.Empty<string>();
	private readonly uint[] _vanillaActors;

	private readonly string[] _validSubDirs = {
		"content", "aoc",
		"01007EF00011E000", "01007EF00011F001"
	};

    public ModFolder(string path, bool auto)
	{
		bool isDirOk = false;
		IEnumerable<string?> subDirs = Directory.GetDirectories(path).Select(x => Path.GetFileName(x).ToLower());
		foreach (var dir in _validSubDirs) {
			if (subDirs.Contains(dir)) {
				isDirOk = true;
				break;
			}
		}

		if (!isDirOk) {
			throw new ArgumentException(
				$"Invalid mod directory, could not find any of the following directories: '{string.Join(", ", _validSubDirs)}'", nameof(path));
		}
		
        _path = path;
		_auto = auto;

		_modActorsPath = Path.Combine(_path, "content", "Actors");
		Directory.CreateDirectory(_modActorsPath);
		_modActors = Directory.EnumerateFiles(_modActorsPath).Select(
			x => Path.GetFileNameWithoutExtension(x)!).ToList();

		_vanillaActors = Array.Empty<uint>(); // load from resource

        // Load the mod actorinfo
        string actorInfoPath = Path.Combine(_path, "content", "Actors", "ActorInfo.product.sbyml");
        Span<byte> actorInfoData = Yaz0.Decompress(actorInfoPath);
        BymlFile actorInfo = new(actorInfoData.ToArray());

		// Load configs
		string ignoreListJson = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? string.Empty, "Data", "Ignored.json");
		if (File.Exists(ignoreListJson)) {
			_ignoredActors = JsonSerializer.Deserialize<string[]>(File.ReadAllText(ignoreListJson))!;
        }

		_cActorsPath = "";
		_cActors = Directory.EnumerateFiles(_cActorsPath).Select(x => Path.GetFileNameWithoutExtension(x)!).ToArray();
    }

	public Task Compute()
	{
		return IterateFolders(_path);
	}

	private async Task IterateFolders(string path)
	{
		await IterateFiles(path);
		await Parallel.ForEachAsync(Directory.EnumerateDirectories(path), async (folder, cancellationToken) => {
			await IterateFolders(path);
		});
	}

	private Task IterateFiles(string path)
	{
        return Parallel.ForEachAsync(Directory.EnumerateFiles(path, "*.smubin"), async (file, cancellationToken) => {
			await ProcessMubin(file);
        });
    }

	private Task ProcessMubin(string path)
	{
		Span<byte> mubinData = Yaz0.Decompress(path);
		BymlNode mubin = new BymlFile(mubinData.ToArray()).RootNode;

		foreach (var obj in mubin.Hash["Objs"].Array.Select(x => x.Hash)) {
			string name = obj["name"].String;

			// Ignore the actor if it's flaged
			// to be ignored or already exists
			if (_ignoredActors.Contains(name) || _modActors.Contains(name)) {
				continue;
			}

			if (_cActors.Contains(name)) {
				File.Copy(Path.Combine(_cActorsPath, name, ".sbactorpack"), Path.Combine(_modActorsPath, name, ".sbactorpack"));
				// update actor info
			}
		}

		return Task.CompletedTask;
	}
}
