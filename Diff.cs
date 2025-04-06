using System.Diagnostics;
using LibGit2Sharp;

namespace EyePatch
{
    internal class Diff
    {
        private static string FindVSCodeCmdPath()
        {
            // Common installation directories for Visual Studio Code
            string[] possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code",
                    "bin", "code.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft VS Code",
                    "bin", "code.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs",
                    "Microsoft VS Code", "bin", "code.cmd")
            };

            // Check each possible path for code.cmd
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new InvalidOperationException(
                "Visual Studio Code is not found. Please ensure it is installed and accessible.");
        }

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

            // Display the number of files in the patch
            Console.WriteLine($"\nNumber of files in the patch: {patch.Count()}");

            Console.WriteLine("\nGit Diff:");
            foreach (var entry in patch)
            {
                Console.WriteLine($"File: {entry.Path}");
                Console.WriteLine($"Status: {entry.Status}");
            }


            // Create a temporary folder to store original files
            string tempFolder = Path.Combine(Path.GetTempPath(), "EyePatch-Diff");

            // Clear the directory if it already exists
            if (Directory.Exists(tempFolder))
            {
                DirectoryInfo di = new DirectoryInfo(tempFolder);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(tempFolder);
            }

            // Find the list of modified files in the current branch
            var changes = repo.Diff.Compare<TreeChanges>(
                    parentCommit.Tree,
                    currentBranch.Tip.Tree);

            List<string> modifiedFiles = new List<string>();
            Console.WriteLine("Modified Files:");
            foreach (var change in changes)
            {
                if (change.Status == ChangeKind.Modified || change.Status == ChangeKind.Added ||
                    change.Status == ChangeKind.Deleted)
                {
                    Console.WriteLine($"{change.Path} ({change.Status})");
                    modifiedFiles.Add(change.Path);
                }
            }

            Console.WriteLine("\nOpening Diffs in VS Code:");

            string workingDirectory = Path.GetDirectoryName(repoPath);

            string vscodePath = FindVSCodeCmdPath();
            Console.WriteLine($"VS Code Path: {vscodePath}");

            // For each modified file, save the original version to the temp folder and open a diff
            foreach (var filePath in modifiedFiles)
            {
                try
                {
                    string tempFilePath =
                        Path.Combine(tempFolder, filePath.Replace("/", "_")); // Save with a unique file name
                    string currentFilePath =
                        Path.Combine(workingDirectory, filePath.Replace("/", "\\")); // Current modified file path

                    // Extract the original file content to a temp file
                    Blob originalBlob = parentCommit[filePath]?.Target as Blob;
                    if (originalBlob != null)
                    {
                        File.WriteAllText(tempFilePath, originalBlob.GetContentText());
                    }
                    else
                    {
                        Console.WriteLine($"Skipping {filePath}, as no original version found.");
                        continue;
                    }

                    // Launch VS Code to show the diff
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = vscodePath,
                        Arguments = $"--diff \"{tempFilePath}\" \"{currentFilePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    });

                    Console.WriteLine($"Opened diff for: {filePath} vs. {currentFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening diff for {filePath}: {ex.Message}");
                }
            }

            Console.WriteLine($"\nOriginal files written to temporary folder: {tempFolder}");
        }
    }
}