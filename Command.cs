using LibGit2Sharp;
using System.Diagnostics;

namespace EyePatch
{
    public abstract class Command
    {
        public abstract void Execute(Settings settings, string? arg = null);

        internal virtual string EnsurePatchesDirectoryExists(Settings settings)
        {
            var patchesPath = settings.PatchDirectory;

            if (string.IsNullOrEmpty(patchesPath))
            {
                // Fallback to OneDrive path from the environment variable
                var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");

                if (string.IsNullOrEmpty(oneDrivePath))
                {
                    throw new InvalidOperationException("OneDrive is not configured on this system. Either install it or configure a patch directory.");
                }

                // Construct the full path to the "patches" directory
                patchesPath = Path.Combine(oneDrivePath, "patches");
            }

            // Ensure the "patches" directory exists
            if (!Directory.Exists(patchesPath))
            {
                Directory.CreateDirectory(patchesPath);
            }

            // Return the full path
            return patchesPath;
        }
        internal virtual void LaunchDiffTool(Settings settings, string tempFolder, List<string> diffFilePairs)
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

        internal virtual string CreateTempFolder()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), $"EyePatch-{Guid.NewGuid()}");

            // Clear the directory if it already exists
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }

            Directory.CreateDirectory(tempFolder);

            return tempFolder;
        }

        internal virtual void DeleteTempFolder(string tempFolder)
        {
            // Clean up the temporary folder
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }

        internal virtual Repository FindRepository()
        {
            Repository repo;
            try
            {
                repo = new Repository(Environment.CurrentDirectory);
            }
            catch (RepositoryNotFoundException e)
            {
                throw new EyePatchException("Not in a Git repository.", e);
            }

            return repo;
        }
    }
}