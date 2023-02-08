using ActorLoader;

string path = Directory.GetCurrentDirectory();
bool auto = false;

// Parse the application args
if (args.Length > 0) {
    path = args[0];

    foreach (var arg in args[1..]) {
        string argName = arg.ToLower().Replace("-", string.Empty);
        if (argName == nameof(auto)) {
            auto = true;
        }
    }
}

// Initialize the ModFolder
ModProcessor folder = new(path, auto);
await folder.Compute();

Console.WriteLine("Task completed succefully!");