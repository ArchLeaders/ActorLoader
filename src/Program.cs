// <path> [-a|--auto] [-b|--be]

using ActorLoader;

string path = Directory.GetCurrentDirectory();
bool auto = false;

// Parse the application args
if (args.Length > 0) {
    path = args[0];
}

foreach (var arg in args[1..]) {
    string argName = arg.ToLower().Replace("-", string.Empty);
    if (argName == nameof(auto)) {
        throw new NotSupportedException("Automatic diffing is not support ed yet");
    }
}

// Initialize the ModFolder
ModFolder folder = new(path, auto);
folder.Compute();

Console.WriteLine("Task completed succefully!");