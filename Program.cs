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
            ConsoleWriter.WriteInfo("");
            ConsoleWriter.WriteInfo("Commands:");
            ConsoleWriter.WriteInfo("  save   Save the resultant committed changes in a branch since it forked, plus any current changes (staged or not)");
            ConsoleWriter.WriteInfo("         producing a single patch. Optionally specify a name for the patch file.");
            ConsoleWriter.WriteInfo("  diff   Show the differences of the resultant committed changes in a branch since it forked, plus any current");
            ConsoleWriter.WriteInfo("         changes (staged or not).");
            ConsoleWriter.WriteInfo("");
            ConsoleWriter.WriteInfo("Options:");
            ConsoleWriter.WriteInfo("  -n, --name   Optional name for the patch file (used with 'save' command), otherwise the branch name is used.");
        }

        public static void Main(string[] args)
        {
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
                        Save.Execute(options.Name);
                    }
                    else if (options.Command.Equals("diff", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Diff.Execute();
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
