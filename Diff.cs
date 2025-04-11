using System.Diagnostics;
using LibGit2Sharp;

namespace EyePatch
{
    internal static class Diff
    {
        public static void Execute()
        {
            // Set the path to the repository
            var repoPath = Path.GetDirectoryName(Repository.Discover(Environment.CurrentDirectory));

            if (repoPath == null)
            {
                ConsoleWriter.WriteError("Not inside a Git repository.");
                return;
            }

            // Open the repository
            using var repo = new Repository(repoPath);

            // Get the current branch
            var currentBranch = repo.Head;
            ConsoleWriter.WriteInfo($"Current Branch: {currentBranch.FriendlyName}");

            // Find the commit where the current branch diverged from its parent branch
            var parentCommit = repo.ObjectDatabase.FindMergeBase(
                currentBranch.Tip,
                repo.Branches["origin/main"].Tip);

            if (parentCommit == null)
            {
                ConsoleWriter.WriteError("Could not determine the parent commit.");
                return;
            }

            ConsoleWriter.WriteInfo($"Parent Commit: {parentCommit.Sha}");

            // Create a temporary folder to store original files
            var tempFolder = Path.Combine(Path.GetTempPath(), "EyePatch-Diff");

            // Clear the directory if it already exists
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
             
            Directory.CreateDirectory(tempFolder);

            // Find the list of modified files in the current branch
            var changes = repo.Diff.Compare<TreeChanges>(
                    parentCommit.Tree,
                    currentBranch.Tip.Tree);

            if (changes is null)
            {
                ConsoleWriter.WriteWarning("No changes to diff.");
                return;
            }

            if (changes.Count == 0)
            {
                ConsoleWriter.WriteWarning("No changes to diff.");
                return;
            }

            List<string> modifiedFiles = [];
            ConsoleWriter.WriteInfo($"\nFiles ({changes.Count}):\n");
            foreach (var change in changes)
            {
                if (change.Status is ChangeKind.Modified or ChangeKind.Added or ChangeKind.Deleted)
                {
                    ConsoleWriter.WriteInfo($"{change.Path} ({change.Status})");
                    modifiedFiles.Add(change.Path);
                }
            }

            ConsoleWriter.WriteNewLine();

            var workingDirectory = Path.GetDirectoryName(repoPath) ?? string.Empty;
            if (string.IsNullOrEmpty(workingDirectory))
            {
                ConsoleWriter.WriteError("Working directory not found.");
                return;
            }

            List<string> diffFilePairs = [];

            foreach (var modifiedFile in modifiedFiles)
            {
                try
                {
                    var tempFilePath =
                        Path.Combine(tempFolder, modifiedFile.Replace("/", "_"));
                    var currentFilePath =
                        Path.Combine(workingDirectory, modifiedFile.Replace("/", "\\"));

                    // Extract the original file content to a temp file
                    var originalBlob = parentCommit[modifiedFile]?.Target as Blob;
                    if (originalBlob != null)
                    {
                        File.WriteAllText(tempFilePath, originalBlob.GetContentText());
                    }
                    else
                    {
                        ConsoleWriter.WriteWarning($"Skipping {modifiedFile}, as no original version found.");
                        continue;
                    }

                    // Check if the two files are identical
                    if (File.Exists(currentFilePath))
                    {
                        var currentFileContent = File.ReadAllText(currentFilePath);
                        if (originalBlob.GetContentText() == currentFileContent)
                        {
                            ConsoleWriter.WriteWarning($"Skipping {modifiedFile} as it is identical (changes were reverted).");
                            continue;
                        }
                    }

                    diffFilePairs.Add($"{tempFilePath} {currentFilePath}");
                }
                catch (Exception ex)
                {
                    ConsoleWriter.WriteError($"Error processing diff for {modifiedFile}: {ex.Message}");
                }
            }

            // Write the file pairs to a temporary file
            var diffFileListPath = Path.Combine(tempFolder, "diffFileList.txt");
            File.WriteAllLines(diffFileListPath, diffFilePairs);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c type \"{diffFileListPath}\" | sdvdiff -i-",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            ConsoleWriter.WriteSuccess($"\nAll done. Waiting on diff window to close...");

            process.Start();
            process.WaitForExit();

            // Clean up the temporary folder
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }
}