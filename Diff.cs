﻿using LibGit2Sharp;

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

            try
            {
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

                List<string> diffFilePairs = [];

                foreach (var modifiedFile in modifiedFiles)
                {
                    try
                    {
                        var tempFilePath =
                            Path.Combine(tempFolder, modifiedFile.Replace("/", "_"));
                        var currentFilePath =
                            Path.Combine(repo.Info.WorkingDirectory, modifiedFile.Replace("/", "\\"));

                        // Extract the original file content to a temp file
                        var originalBlob = parentCommit[modifiedFile]?.Target as Blob;
                        if (originalBlob != null)
                        {
                            WriteBlobAsFile(tempFilePath, originalBlob);
                        }
                        else
                        {
                            ConsoleWriter.WriteWarning($"Skipping {modifiedFile}, as no original version found.");
                            continue;
                        }

                        if (AreFilesIdentical(currentFilePath, originalBlob, modifiedFile))
                        {
                            ConsoleWriter.WriteWarning(
                                $"Skipping {modifiedFile} as it is identical (changes were reverted).");
                            continue;
                        }

                        diffFilePairs.Add($"{tempFilePath} {currentFilePath}");
                    }
                    catch (Exception e)
                    {
                        throw new EyePatchException($"Error processing diff for {modifiedFile}: {e.Message}", e);
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