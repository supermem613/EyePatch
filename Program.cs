using CommandLine;

namespace EyePatch
{
    internal static class Program
    {
        private class Options
        {
            [Value(0, MetaName = "command", Required = true, HelpText = "The command to execute (e.g., save).")]
            public required string Command { get; set; }

            [Option('n', "name", Required = false, HelpText = "Optional name for the patch file.")]
            public required string Name { get; set; }
        }
        
        private static void ShowHelp()
        {
            ConsoleWriter.WriteInfo("Usage: EyePatch <command> [options]");
            ConsoleWriter.WriteNewLine();
            ConsoleWriter.WriteInfo("Commands:");
            ConsoleWriter.WriteInfo("  save   Save the resultant committed changes in a branch since it forked, plus any current changes (staged or not)");
            ConsoleWriter.WriteInfo("         producing a single patch. Optionally specify a name for the patch file.");
            ConsoleWriter.WriteNewLine();
            ConsoleWriter.WriteInfo("  diff   Show the differences of the resultant committed changes in a branch since it forked, plus any current");
            ConsoleWriter.WriteInfo("         changes (staged or not).");
            ConsoleWriter.WriteNewLine();
            ConsoleWriter.WriteInfo("  view   Show the differences of a given patch file against its base commit");
            ConsoleWriter.WriteNewLine();
            ConsoleWriter.WriteInfo("Options:");
            ConsoleWriter.WriteInfo("  -n, --name   Optional name for the patch file (used with 'save' command), otherwise the branch name is used.");
            ConsoleWriter.WriteNewLine();
            ConsoleWriter.WriteInfo("Settings:");
            ConsoleWriter.WriteInfo("  Configured in the .eyepatch.settings file in your user folder, as a JSON file.");
            ConsoleWriter.WriteInfo("     DiffApp: The diff application to use (default: windiff).");
            ConsoleWriter.WriteNewLine();
        }

        public static void Main(string[] args)
        {
            var settings = Settings.Load();
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            Parser.Default.ParseArguments<Program.Options>(args)
                .WithParsed(options =>
                {
                    ArgumentNullException.ThrowIfNull(options);

                    if (options.Command.Equals("save", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Save.Execute(options.Name, settings);
                    }
                    else if (options.Command.Equals("diff", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Diff.Execute(settings);
                    }
                    else if (options.Command.Equals("view", StringComparison.CurrentCultureIgnoreCase))
                    {
                        View.Execute(options.Name, settings);
                    }
                    else
                    {
                        ConsoleWriter.WriteError("Unknown command.");
                    }
                })
                .WithNotParsed(errors =>
                {
                    ConsoleWriter.WriteError("Failed to parse arguments. Use '--help' for usage instructions.");
                });
        }
    }
}
