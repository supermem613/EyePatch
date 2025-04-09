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
            Console.WriteLine("Usage: EyePatch <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  save   Save the current state to a file in OneDrive. Optionally specify a name for the patch file.");
            Console.WriteLine("  diff   Show the differences.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -n, --name   Optional name for the patch file (used with 'save' command), otherwise the branch name is used.");
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
                        Console.WriteLine("Unknown command.");
                    }
                })
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("Failed to parse arguments. Use '--help' for usage instructions.");
                });
        }
    }
}
