using LibGit2Sharp;
using System;

namespace EyePatch
{
    class Program
    {
        static void Main(string[] args)
        {
            // Set the path to the repository
            string repoPath = Environment.CurrentDirectory;

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
//                Console.WriteLine("\n--- Diff ---");
//                Console.WriteLine(entry.Patch);
//                Console.WriteLine("------------\n");
            }

            // Write the diff to a patch file
            string patchFileName = "diff.patch";
            using (StreamWriter writer = new StreamWriter(patchFileName))
            {
                writer.Write(patch.Content); // Write the entire patch content
            }

            Console.WriteLine($"\nPatch file written: {patchFileName}");
        }
    }
}
