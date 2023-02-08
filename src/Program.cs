using ActorLoader;

string path = Directory.GetCurrentDirectory();
bool auto = false;
bool parallel = false;

// Parse the application args
if (args.Length > 0) {
    path = args[0];

    foreach (var arg in args[1..].Where(x => x.StartsWith('-'))) {
        string argName = arg.ToLower()[1..];
        if (argName == nameof(auto) || argName[0] == nameof(auto)[0]) {
            auto = true;
        }
        else if (argName == nameof(parallel) || argName[0] == nameof(parallel)[0]) {
            parallel = true;
        }
    }
}

// Initialize the ModFolder
ModProcessor folder = new(path, auto);
await folder.Compute();

Console.WriteLine("Task completed succefully!");