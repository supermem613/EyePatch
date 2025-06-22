using LibGit2Sharp;

namespace EyePatch
{
    internal class Diff : Command
    {
        public override void Execute(Settings settings, string? arg = null)
        {
            var repo = FindRepository();

            ExecuteWithRepo(
                settings,
                repo);
        }

        internal void ExecuteWithRepo(
            Settings settings,
            IRepository repo)
        {
            // Get the current branch
            var currentBranch = repo.Head;
            ConsoleWriter.WriteInfo($"Current Branch: {currentBranch.FriendlyName}");

            // Find the commit where the current branch diverged from its parent branch
            var parentCommit = repo.ObjectDatabase.FindMergeBase(
                currentBranch.Tip,
                repo.Branches["origin/main"].Tip);

            if (null == parentCommit)
            {
                throw new EyePatchException("Could not determine the parent commit.");
            }

            ConsoleWriter.WriteInfo($"Parent Commit: {parentCommit.Sha}");

            // Create a temporary folder to store original files
            var tempFolder = CreateTempFolder();

            Dictionary<string, ChangeKind> changes = new Dictionary<string, ChangeKind>();

            try
            {
                // Find the list of modified files in the current branch
                var changesFromTip = repo.Diff.Compare<TreeChanges>(
                    parentCommit.Tree,
                    currentBranch.Tip.Tree);

                if (changesFromTip != null)
                {
                    foreach (var change in changesFromTip)
                    {
                        changes.Add(change.Path, change.Status);
                    }
                }

                // Get all modified, added, or deleted files in the working directory (staged or unstaged)
                var statusEntries = repo.RetrieveStatus(new StatusOptions());

                if (statusEntries != null)
                {
                    foreach (var entry in statusEntries)
                    {
                        if (entry.State.HasFlag(FileStatus.ModifiedInWorkdir) ||
                            entry.State.HasFlag(FileStatus.ModifiedInIndex))
                        {
                            changes[entry.FilePath] = ChangeKind.Modified;
                        }
                        else if (entry.State.HasFlag(FileStatus.NewInIndex))
                        {
                            changes.Add(entry.FilePath, ChangeKind.Added);
                        }
                        else if (entry.State.HasFlag(FileStatus.DeletedFromWorkdir) ||
                            entry.State.HasFlag(FileStatus.DeletedFromIndex))
                        {
                            changes[entry.FilePath] = ChangeKind.Deleted;
                        }
                    }
                }

                if (changes.Count == 0)
                {
                    ConsoleWriter.WriteWarning("No changes to diff.");
                    return;
                }

                ConsoleWriter.WriteInfo($"\nFiles ({changes.Count}):\n");

                ConsoleWriter.WriteNewLine();

                List<string> diffFilePairs = [];

                foreach (var change in changes)
                {
                    if (change.Value is ChangeKind.Modified or ChangeKind.Added or ChangeKind.Deleted)
                    {
                        ConsoleWriter.WriteInfo($"{change.Key} ({change.Value})");

                        try
                        {
                            var tempFilePath =
                                Path.Combine(tempFolder, change.Key.Replace("/", "_"));
                            var currentFilePath =
                                Path.Combine(repo.Info.WorkingDirectory, change.Key.Replace("/", "\\"));

                            // Extract the original file content to a temp file
                            var originalBlob = parentCommit[change.Key]?.Target as Blob;
                            if (originalBlob != null)
                            {
                                WriteBlobAsFile(tempFilePath, originalBlob);
                            }
                            else
                            {
                                ConsoleWriter.WriteWarning($"Skipping {change.Key}, as no original version found.");
                                continue;
                            }

                            if (AreFilesIdentical(currentFilePath, originalBlob, change.Key))
                            {
                                ConsoleWriter.WriteWarning(
                                    $"Skipping {change.Key} as it is identical (changesFromTip were reverted).");
                                continue;
                            }

                            diffFilePairs.Add($"{tempFilePath} {currentFilePath}");
                        }
                        catch (Exception e)
                        {
                            throw new EyePatchException($"Error processing diff for {change.Key}: {e.Message}", e);
                        }
                    }
                }

                LaunchDiffTool(settings, tempFolder, diffFilePairs);
            }
            finally
            {
                DeleteTempFolder(tempFolder);
            }
        }
        internal virtual bool AreFilesIdentical(string currentFilePath, Blob originalBlob, string modifiedFile)
        {
            // Check if the two files are identical
            if (File.Exists(currentFilePath))
            {
                var currentFileContent = File.ReadAllText(currentFilePath);
                if (originalBlob.GetContentText() == currentFileContent)
                {
                    return true;
                }
            }

            return false;
        }

        internal virtual void WriteBlobAsFile(string tempFilePath, Blob originalBlob)
        {
            File.WriteAllText(tempFilePath, originalBlob.GetContentText());
        }
    }
}