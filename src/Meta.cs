namespace ActorLoader;

public static class Meta
{
    public static string Version { get; } = typeof(Program).Assembly.GetName().Version!.ToString(3);
    public static string Help { get; } = $"""
         Actor Loader, v{Version}
         
         (c) ArchLeaders 2023, under the AGPL-3.0 license
         - - - - - - - - - - - - - - - - - - - - - - - - -
         
         
         ActorLoader.exe <path> [-a|--auto] [-p|--parallel]
         
         
         path: The path to the root mod folder (e.g. the
             folder containing 'content')
         
         auto: Automatically append C to valid actors that
             don't exist in the vanilla game files
         
         parallel: Iterate the mod directories in parallel (asynchronously)
                 Warning: this can cause issues if some actors
                 weren't previously downloaded
        """;
}
