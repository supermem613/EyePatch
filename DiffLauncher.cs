using System.Diagnostics;

namespace EyePatch
{
    internal static class DiffLauncher
    {
        public static void LaunchDiffTool(
            Settings settings,
            string tempFolder,
            List<string> diffFilePairs)
        {
            var diffFileListPath = Path.Combine(tempFolder, "diff_file_list.txt");
            File.WriteAllLines(diffFileListPath, diffFilePairs);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {settings.DiffApp} -I \"{diffFileListPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            ConsoleWriter.WriteSuccess($"\nAll done. Waiting on diff window to close...");

            process.Start();
            process.WaitForExit();
        }
    }
}