using LibGit2Sharp;

namespace EyePatch
{
    internal static class Save
    {
        private static string EnsurePatchesDirectoryExists()
        {
            // Retrieve the OneDrive path from the environment variable
            var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");

            if (string.IsNullOrEmpty(oneDrivePath))
            {
                throw new InvalidOperationException("OneDrive is not configured on this system.");
            }

            // Construct the full path to the "patches" directory
            var patchesPath = Path.Combine(oneDrivePath, "patches");

            // Ensure the "patches" directory exists
            if (!Directory.Exists(patchesPath))
            {
                Directory.CreateDirectory(patchesPath);
            }

            // Return the full path
            return patchesPath;
        }

        public static void Execute(string patchFileName)
        {
            // Set the path to the repository
            var repoPath = Repository.Discover(Environment.CurrentDirectory);

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

            // Generate the diff based on the parent commit
            var diffOptions = new CompareOptions();
            var patch = repo.Diff.Compare<Patch>(
                parentCommit.Tree,
                currentBranch.Tip.Tree,
                diffOptions);

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
                EnsurePatchesDirectoryExists(),
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

