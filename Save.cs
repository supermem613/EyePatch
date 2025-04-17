using LibGit2Sharp;

namespace EyePatch
{
    internal class Save
    {
        public void Execute(string patchFileName, Settings settings)
        {
            Repository repo;
            try
            {
                repo = new Repository(Environment.CurrentDirectory);
            }
            catch (LibGit2Sharp.RepositoryNotFoundException e)
            {
                throw new EyePatchException("Not in a Git repository.", e);
            }

            ExecuteWithRepo(
                patchFileName,
                settings,
                repo);
        }

        internal void ExecuteWithRepo(
            string patchFileName,
            Settings settings,
            IRepository repo)
        {
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

            if (null == parentCommit)
            {
                throw new EyePatchException("Could not determine the parent commit.");
            }

            ConsoleWriter.WriteInfo($"Parent Commit: {parentCommit.Sha}");

            // Generate the diff based on the parent commit, including staged and unstaged changes
            var patch = repo.Diff.Compare<Patch>(
                parentCommit.Tree,
                DiffTargets.Index | DiffTargets.WorkingDirectory);

            int count = 0;

            foreach (var entry in patch)
            {
                ConsoleWriter.WriteInfo($"File: {entry.Path}");
                ConsoleWriter.WriteInfo($"Status: {entry.Status}");
                count++;
            }

            if (count == 0)
            {
                ConsoleWriter.WriteWarning("No changes to save.");
                return;
            }

            patchFileName = Path.Combine(
                PatchDirectory.EnsurePatchesDirectoryExists(settings),
                string.Concat(
                    patchFileName,
                    ".",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"),
                    ".patch"));

            WriteAndVerifyPatchFile(patchFileName, patch);

            ConsoleWriter.WriteSuccess($"\nPatch file written: \"{patchFileName}\"");
        }

        internal virtual void WriteAndVerifyPatchFile(string patchFileName, Patch patch)
        {
            using (var writer = new StreamWriter(patchFileName))
            {
                writer.Write(patch.Content);
            }

            if (!File.Exists(patchFileName) || new FileInfo(patchFileName).Length == 0)
            {
                throw new EyePatchException("Patch file was not created or is empty.");
            }
        }
    }
}