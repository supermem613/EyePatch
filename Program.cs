using CommandLine;

namespace EyePatch
{
    internal static class Program
    {
        private class Options
        {
            [Value(0, MetaName = "command", Required = true, HelpText = "The command to execute (e.g., save).")]
            public required string Command { get; init; }

            [Value(1, MetaName = "name", Required = false, HelpText = "Optional name for the patch file.")]
            public required string Name { get; init; }
        }
        
        private static void ShowHelp()
        {
            ConsoleWriter.WriteInfo(@"
    Usage: EyePatch <command> [options]

    Go to https://github.com/supermem613/EyePatch for more information.

    Commands:
        save   Save the resultant committed changes in a branch since it forked, plus any current changes (staged or not)
                producing a single patch. Optionally specify a name for the patch file.

        diff   Show the differences of the resultant committed changes in a branch since it forked, plus any current
                changes (staged or not).

        view   Show the differences of a given patch file against its base commit. Requires a name argument.

    Settings:
        Configured in the .eyepatch.settings file in your user folder, as a JSON file.

            DiffApp: The diff application to use (default: windiff).
            PatchDirectory: The directory where patches are saved (default: OneDrive\patches).
            ");
        }

        public static void Main(string[] args)
        {
            var settings = Settings.Load();
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options =>
                    {
                        ArgumentNullException.ThrowIfNull(options);
                        if (options.Command.Equals("save", StringComparison.CurrentCultureIgnoreCase))
                        {
                            new Save().Execute(settings, options.Name);
                        }
                        else if (options.Command.Equals("diff", StringComparison.CurrentCultureIgnoreCase))
                        {
                            new Diff().Execute(settings);
                        }
                        else if (options.Command.Equals("view", StringComparison.CurrentCultureIgnoreCase))
                        {
                            new View().Execute(settings, options.Name);
                        }
                        else
                        {
                            throw new EyePatchException("Unknown command.");
                        }
                    })
                    .WithNotParsed(_ => throw new EyePatchException("Failed to parse arguments."));
            }
            catch (EyePatchException ex)
            {
                ConsoleWriter.WriteError(ex.Message);
            }
        }
    }
}
