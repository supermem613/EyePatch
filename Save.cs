using LibGit2Sharp;

namespace EyePatch
{
    internal static class Save
    {
        public static void Execute(string patchFileName, Settings settings)
        {
            IRepository repo;
            try
            {
                repo = new Repository(Environment.CurrentDirectory);
            }
            catch (LibGit2Sharp.RepositoryNotFoundException)
            {
                ConsoleWriter.WriteError("Not in a Git repository.");
                return;
            }

            // Get the current branch
            var currentBranch = repo.Head;
            ConsoleWriter.WriteInfo($"Current Branch: {currentBranch.FriendlyName}");

            // Use branch name as default patch file name if none is provided
            if (string.IsNullOrEmpty(patchFileName))
            {
                patchFileName = $"{currentBranch.FriendlyName}";
            }

            if (patchFileName.Contains('/'))
            {
                patchFileName = patchFileName[(patchFileName.LastIndexOf('/') + 1)..];
            }

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

            // Generate the diff based on the parent commit, including staged and unstaged changes
            var patch = repo.Diff.Compare<Patch>(
                parentCommit.Tree,
                DiffTargets.Index | DiffTargets.WorkingDirectory);

            if (!patch.Any())
            {
                ConsoleWriter.WriteWarning("No changes to save.");
                return;
            }

            ConsoleWriter.WriteInfo($"\nFiles ({patch.Count()}):");
            foreach (var entry in patch)
            {
                ConsoleWriter.WriteInfo($"File: {entry.Path}");
                ConsoleWriter.WriteInfo($"Status: {entry.Status}");
            }

            patchFileName = Path.Combine(
                PatchDirectory.EnsurePatchesDirectoryExists(settings),
                string.Concat(
                    patchFileName,
                    ".",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"),
                    ".patch"));

            using (var writer = new StreamWriter(patchFileName))
            {
                writer.Write(patch.Content);
            }

            if (!File.Exists(patchFileName) || new FileInfo(patchFileName).Length == 0)
            {
                ConsoleWriter.WriteError("Patch file was not created or is empty.");
                return;
            }

            ConsoleWriter.WriteSuccess($"\nPatch file written: \"{patchFileName}\"");
        }
    }
}