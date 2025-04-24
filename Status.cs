using LibGit2Sharp;

namespace EyePatch
{
    internal class Status : Command
    {
        public override void Execute(Settings settings, string? arg = null)
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

            ExecuteWithRepo(settings, repo);
        }

        internal void ExecuteWithRepo(Settings settings, IRepository repo)
        {
            var statusOptions = new StatusOptions
            {
                IncludeIgnored = false,
                IncludeUnaltered = false
            };
            var repoStatus = repo.RetrieveStatus(statusOptions);

            TreeChanges? remoteChanges = null;
            var mainBranch = repo.Branches["main"];
            if (mainBranch?.Tip != null && repo.Head.Tip != null)
            {
                try
                {
                    remoteChanges = repo.Diff.Compare<TreeChanges>(
                        repo.Head.Tip.Tree,
                        mainBranch.Tip.Tree
                    );
                }
                catch
                {
                    // ignore diff issues
                }
            }

            // Iterate each file entry in the working status.
            foreach (var entry in repoStatus)
            {
                var filePath = entry.FilePath;
                var color = ConsoleColor.Yellow;
                var isConflict = false;

                // Check if the file is already marked as conflicted.
                if (entry.State.HasFlag(FileStatus.Conflicted))
                {
                    color = ConsoleColor.Magenta;
                    isConflict = true;
                }
                else
                {
                    // Determine color based on working directory change.
                    if (entry.State.HasFlag(FileStatus.NewInWorkdir))
                    {
                        color = ConsoleColor.Green;
                    }
                    else if (entry.State.HasFlag(FileStatus.ModifiedInWorkdir))
                    {
                        color = ConsoleColor.Yellow;
                    }
                    else if (entry.State.HasFlag(FileStatus.DeletedFromWorkdir))
                    {
                        color = ConsoleColor.Red;
                    }
                }

                // If there are remote changes (committed changes past current) for the same file,
                // and the local change is a modification or deletion, mark as conflict.
                if (!isConflict && remoteChanges != null)
                {
                    var remoteChange = remoteChanges.Any(change => change.Path == filePath);
                    if (remoteChange &&
                        (entry.State.HasFlag(FileStatus.ModifiedInIndex) ||
                         entry.State.HasFlag(FileStatus.DeletedFromIndex)))
                    {
                        color = ConsoleColor.Magenta;
                        isConflict = true;
                    }
                }

                Console.ForegroundColor = color;
                Console.WriteLine($"{filePath} {(isConflict ? "(CONFLICT)" : string.Empty)}");
            }

            Console.ResetColor();
        }
    }
}
