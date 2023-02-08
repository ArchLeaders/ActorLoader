using Byml.Security.Cryptography;
using Nintendo.Byml;
using Yaz0Library;

namespace ActorLoader;

public class ModProcessor
{
	private readonly string _path;
	private readonly bool _auto;
	private readonly bool _parallel;

	private readonly string _actorsPath;
	private readonly BymlFile _actorInfo;
	private readonly List<string> _actors;

	private readonly string _srcActorsPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? string.Empty, "Data", "Actors");
	private readonly BymlFile _srcActorInfo = Resource.GetSrcActorInfo();
	private readonly string[] _srcActors;

	private readonly string[] _ignoredActors = Resource.GetIgnoredList();
	private readonly uint[] _vanillaActors = Resource.GetVanillaActorsList();

    public ModProcessor(string path, bool auto)
	{
		CheckModPath(path);
		
        _path = path;
		_auto = auto;

        _actorsPath = Directory.CreateDirectory(Path.Combine(_path, "content", "Actor", "Pack")).FullName;
        _actorInfo = Resource.GetModActorInfo(Path.Combine(_path, "content", "Actor", "ActorInfo.product.sbyml"));
        _actors = Directory.EnumerateFiles(_actorsPath).Select(x => Path.GetFileNameWithoutExtension(x)!).ToList();
		
		_srcActors = _srcActorInfo.RootNode.Hash.Keys.ToArray();
    }

	public async Task Compute()
	{
		await IterateFolders(_path);

		byte[] data = _actorInfo.ToBinary();
		data = Yaz0.Compress(data.AsSpan(), out Yaz0SafeHandle _).ToArray();
		File.WriteAllBytes(Path.Combine(_path, "content", "Actor", "ActorInfo.product.sbyml"), data);
    }

	private async Task IterateFolders(string path)
	{
		await IterateFiles(path);

		IEnumerable<string> iter = Directory.EnumerateDirectories(path);
        if (_parallel) {
			await Parallel.ForEachAsync(iter, async (folder, cancellationToken) => await IterateFolders(folder));
		}
		else {
			foreach (var folder in iter) {
				await IterateFolders(folder);
			}
		}
	}

	private async Task IterateFiles(string path)
	{
		IEnumerable<string> iter = Directory.EnumerateFiles(path, "*.smubin");
        if (_parallel) {
			await Parallel.ForEachAsync(iter, async (file, cancellationToken) => await ProcessMubin(file));
		}
		else {
			foreach (var file in iter) {
				await ProcessMubin(file);
			}
		}
    }

	private async Task ProcessMubin(string path)
	{
		Console.WriteLine($"[Processing] -> '{Path.GetFileNameWithoutExtension(path)}'");
		bool madeChanges = false;

		byte[] mubinData = Yaz0.Decompress(path).ToArray();
		BymlFile mubin = new(mubinData);

		BymlNode diff = null!;

        if (_auto) {
            string map = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)!)!);
            string unit = Path.GetFileName(Path.GetDirectoryName(path)!);
            string type = Path.GetFileNameWithoutExtension(path).Split('_')[1];
			diff = Resource.GetVanillaMapEntry(unit, map, type).RootNode;
        }

		foreach (var obj in mubin.RootNode.Hash["Objs"].Array.Select(x => x.Hash)) {
			string name = obj["UnitConfigName"].String;

			// Ignore the actor if it's flaged
			// to be ignored or already exists
			if (_ignoredActors.Contains(name) || _actors.Contains(name)) {
				continue;
			}

			if (_auto && _srcActors.Contains(name + 'C') && !diff.Hash["Objs"].Array.Select(x => x.Hash["HashId"].UInt).Contains(obj["HashId"].UInt)) {
				obj["UnitConfigName"] = new(name += 'C');
				madeChanges = true;

                Console.WriteLine($"  -> [Auto-Fixed] -> '{name}@{obj["HashId"].UInt}'");
			}

			if (_srcActors.Contains(name) && !_vanillaActors.Contains(Crc32.Compute(name))) {
				string cActor = Path.Combine(_srcActorsPath, $"{name}.sbactorpack");
				if (!File.Exists(cActor)) {
					await Resource.DownloadCActor(cActor, name);
				}

				File.Copy(Path.Combine(_srcActorsPath, $"{name}.sbactorpack"), Path.Combine(_actorsPath, $"{name}.sbactorpack"), true);
				_actorInfo.RootNode.Hash["Actors"].Array.Add(
					_srcActorInfo.RootNode.Hash[name]
				);

				_actors.Add(name);
                Console.WriteLine($"  -> [Updated] -> '{name}'");
			}
		}

		if (madeChanges) {
			byte[] data = mubin.ToBinary();
			data = Yaz0.Compress(data.AsSpan(), out Yaz0SafeHandle _).ToArray();
			File.WriteAllBytes(path, data);
		}
	}

	private readonly string[] _validSubDirs = {
		"content", "aoc",
		"01007EF00011E000", "01007EF00011F001"
	};

	private bool CheckModPath(string path)
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

		return isDirOk;
    }
}
