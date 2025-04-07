using System.Diagnostics;
using LibGit2Sharp;

namespace EyePatch
{
    internal class Diff
    {
        public static void Execute()
        {
            // Set the path to the repository
            string repoPath = Path.GetDirectoryName(Repository.Discover(Environment.CurrentDirectory));

            if (repoPath == null)
            {
                Console.WriteLine("Not inside a Git repository.");
                return;
            }

            // Open the repository
            using var repo = new Repository(repoPath);

            // Get the current branch
            var currentBranch = repo.Head;
            Console.WriteLine($"Current Branch: {currentBranch.FriendlyName}");

            // Find the commit where the current branch diverged from its parent branch
            var parentCommit = repo.ObjectDatabase.FindMergeBase(
                currentBranch.Tip,
                repo.Branches["origin/main"].Tip);

            if (parentCommit == null)
            {
                Console.WriteLine("Could not determine the parent commit.");
                return;
            }

            Console.WriteLine($"Parent Commit: {parentCommit.Sha}");

            // Generate the diff based on the parent commit
            var diffOptions = new CompareOptions();
            var patch = repo.Diff.Compare<Patch>(
                parentCommit.Tree,
                currentBranch.Tip.Tree,
                diffOptions);

            // Create a temporary folder to store original files
            string tempFolder = Path.Combine(Path.GetTempPath(), "EyePatch-Diff");

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

            List<string> modifiedFiles = new List<string>();
            Console.WriteLine($"\nFiles ({patch.Count()}):\n");
            foreach (var change in changes)
            {
                if (change.Status == ChangeKind.Modified || change.Status == ChangeKind.Added ||
                    change.Status == ChangeKind.Deleted)
                {
                    Console.WriteLine($"{change.Path} ({change.Status})");
                    modifiedFiles.Add(change.Path);
                }
            }

            string workingDirectory = Path.GetDirectoryName(repoPath);

            List<string> diffFilePairs = new List<string>();

            foreach (var modifiedFile in modifiedFiles)
            {
                try
                {
                    string tempFilePath =
                        Path.Combine(tempFolder, modifiedFile.Replace("/", "_"));
                    string currentFilePath =
                        Path.Combine(workingDirectory, modifiedFile.Replace("/", "\\"));

                    // Extract the original file content to a temp file
                    Blob originalBlob = parentCommit[modifiedFile]?.Target as Blob;
                    if (originalBlob != null)
                    {
                        File.WriteAllText(tempFilePath, originalBlob.GetContentText());
                    }
                    else
                    {
                        Console.WriteLine($"Skipping {modifiedFile}, as no original version found.");
                        continue;
                    }

                    diffFilePairs.Add($"{tempFilePath} {currentFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing diff for {modifiedFile}: {ex.Message}");
                }
            }

            // Write the file pairs to a temporary file
            string diffFileListPath = Path.Combine(tempFolder, "diffFileList.txt");
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

            Console.WriteLine($"\nWaiting on diff window to close.");

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