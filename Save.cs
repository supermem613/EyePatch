using LibGit2Sharp;

namespace EyePatch
{
    internal class Save
    {
        private static string EnsurePatchesDirectoryExists()
        {
            // Retrieve the OneDrive path from the environment variable
            string oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");

            if (string.IsNullOrEmpty(oneDrivePath))
            {
                throw new InvalidOperationException("OneDrive is not configured on this system.");
            }

            // Construct the full path to the "patches" directory
            string patchesPath = Path.Combine(oneDrivePath, "patches");

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
            string repoPath = Repository.Discover(Environment.CurrentDirectory);

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

            patchFileName = Path.Combine(
                EnsurePatchesDirectoryExists(),
                string.Concat(
                    patchFileName,
                    ".",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"),
                    ".patch"));

            using (StreamWriter writer = new StreamWriter(patchFileName))
            {
                writer.Write(patch.Content);
            }

            Console.WriteLine($"\nPatch file written: {patchFileName}");
        }
    }
}

