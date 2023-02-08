using Byml.Security.Cryptography;
using Nintendo.Byml;
using Yaz0Library;

namespace ActorLoader;

public class ModFolder
{
	private readonly string _path;
	private readonly bool _auto;

	private readonly string _actorsPath;
	private readonly BymlFile _actorInfo;
	private readonly List<string> _actors;

	private readonly string _srcActorsPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? string.Empty, "Data", "Actors");
	private readonly BymlFile _srcActorInfo = Resource.GetSrcActorInfo();
	private readonly List<string> _srcActors;

	private readonly string[] _ignoredActors = Resource.GetIgnoredList();
	private readonly uint[] _vanillaActors = Resource.GetVanillaActorsList();

    public ModFolder(string path, bool auto)
	{
		CheckModPath(path);
		
        _path = path;
		_auto = auto;

        _actorsPath = Directory.CreateDirectory(Path.Combine(_path, "content", "Actor", "Pack")).FullName;
        _actorInfo = Resource.GetModActorInfo(Path.Combine(_path, "content", "Actor", "ActorInfo.product.sbyml"));
        _actors = Directory.EnumerateFiles(_actorsPath).Select(x => Path.GetFileNameWithoutExtension(x)!).ToList();
		
		_srcActors = _srcActorInfo.RootNode.Hash.Keys.ToList();
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
		await Parallel.ForEachAsync(Directory.EnumerateDirectories(path), async (folder, cancellationToken) => {
			await IterateFolders(folder);
		});
	}

	private Task IterateFiles(string path)
	{
        return Parallel.ForEachAsync(Directory.EnumerateFiles(path, "*.smubin"), async (file, cancellationToken) => {
			await ProcessMubin(file);
        });
    }

	private async Task ProcessMubin(string path)
	{
		Console.WriteLine($"[{DateTime.Now:u}] [Processing] | '{Path.GetFileNameWithoutExtension(path)}'");

		byte[] mubinData = Yaz0.Decompress(path).ToArray();
		BymlNode mubin = new BymlFile(mubinData).RootNode;

		BymlNode? diff = null;

        if (_auto) {
            string map = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)!)!);
            string unit = Path.GetFileName(Path.GetDirectoryName(path)!);
            string type = Path.GetFileName(path).Split('_')[1];
			diff = Resource.GetVanillaMapEntry(unit, map, type).RootNode;
        }

		foreach (var obj in mubin.Hash["Objs"].Array.Select(x => x.Hash)) {
			string name = obj["UnitConfigName"].String;

			// Ignore the actor if it's flaged to
			// be ignored, already exists or is a
			// vanilla actor.
			if (_vanillaActors.Contains(Crc32.Compute(name)) || _ignoredActors.Contains(name) || _actors.Contains(name)) {
				continue;
			}

			if (_auto && _srcActors.Contains(name + 'C')) {
				name += 'C';
				obj["UintConfigName"] = new(name);

				// Check if it's a vanilla entry
				if (diff!.Hash["Objs"].Array.Select(x => x.Hash["HashID"].UInt).Contains(obj["HashID"].UInt)) {
					continue;
				}
			}

			if (_srcActors.Contains(name)) {
				string cActor = Path.Combine(_srcActorsPath, $"{name}.sbactorpack");
				if (!File.Exists(cActor)) {
					await Resource.DownloadCActor(cActor, name);
				}

				File.Copy(Path.Combine(_srcActorsPath, $"{name}.sbactorpack"), Path.Combine(_actorsPath, $"{name}.sbactorpack"), true);
				_actorInfo.RootNode.Hash["Actors"].Array.Add(
					_srcActorInfo.RootNode.Hash[name]
				);

				_srcActors.Add(name);
                Console.WriteLine($"  -> [{DateTime.Now:u}] [Updated] -> '{name}'");
			}
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
