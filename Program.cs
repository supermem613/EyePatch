using CommandLine;

namespace EyePatch
{
    class Program
    {
        class Options
        {
            [Value(0, MetaName = "command", Required = true, HelpText = "The command to execute (e.g., save).")]
            public string Command { get; set; }

            [Option('n', "name", Required = false, HelpText = "Optional name for the patch file.")]
            public string Name { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Program.Options>(args)
                .WithParsed(options =>
                {
                    if (options.Command.ToLower() == "save")
                    {
                        Save.Execute(options.Name);
                    }
                    if (options.Command.ToLower() == "diff")
                    {
                        Diff.Execute();
                    }
                    else
                    {
                        Console.WriteLine("Unknown command. Supported commands: save");
                    }
                })
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("Failed to parse arguments. Use '--help' for usage instructions.");
                });
        }
    }
}
