using System.Diagnostics;
using System.Text;
using LibGit2Sharp;

namespace EyePatch
{
    internal static class View
    {
        public static void Execute(string patchFilePath)
        {
            if (!File.Exists(patchFilePath))
            {
                ConsoleWriter.WriteError($"Patch file not found: {patchFilePath}");
                return;
            }

            // Create a temporary folder to store the base and patched files
            var tempFolder = Path.Combine(Path.GetTempPath(), "EyePatch-View");
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
            Directory.CreateDirectory(tempFolder);

            try
            {
                // Read the patch file
                var patchContent = File.ReadAllText(patchFilePath);

                // Parse the patch file
                var patchParser = new PatchParser(patchContent);
                var patches = patchParser.Parse();

                // Open the repository
                var repoPath = Repository.Discover(Environment.CurrentDirectory);
                if (repoPath == null)
                {
                    ConsoleWriter.WriteError("Not inside a Git repository.");
                    return;
                }

                using var repo = new Repository(repoPath);

                foreach (var patch in patches)
                {
                    // Get the base file content using the index hash
                    var blob = repo.Lookup<Blob>(patch.BaseIndex);
                    if (blob == null)
                    {
                        ConsoleWriter.WriteError($"Base file not found for {patch.BaseFilePath}.");
                        continue;
                    }

                    // Write the base file content to a temporary file
                    var baseFilePath = Path.Combine(tempFolder, Path.GetFileName(patch.BaseFilePath));
                    File.WriteAllText(baseFilePath, blob.GetContentText());

                    // Apply the patch to create the patched version
                    var patchedContent = PatchApplier.Apply(blob.GetContentText(), patch.DiffContent);

                    // Write the patched content to another temporary file
                    var patchedFilePath = Path.Combine(tempFolder, "patched_" + Path.GetFileName(patch.BaseFilePath));
                    File.WriteAllText(patchedFilePath, patchedContent);

                    // Perform a diff between the base and patched versions
                    ConsoleWriter.WriteInfo($"Diffing {baseFilePath} against {patchedFilePath}...");
                    var diffProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c sdvdiff \"{baseFilePath}\" \"{patchedFilePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    diffProcess.Start();
                    Console.WriteLine(diffProcess.StandardOutput.ReadToEnd());
                    Console.WriteLine(diffProcess.StandardError.ReadToEnd());
                    diffProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteError($"Error processing patch file: {ex.Message}");
            }
            finally
            {
                // Clean up the temporary folder
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }
    }

    internal class PatchParser(string patchContent)
    {
        private readonly string _patchContent = patchContent;

        public List<FilePatch> Parse()
        {
            var patches = new List<FilePatch>();
            var lines = _patchContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            FilePatch? currentPatch = null;
            var diffContentBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("diff --git"))
                {
                    if (currentPatch != null)
                    {
                        currentPatch.DiffContent = diffContentBuilder.ToString();
                        patches.Add(currentPatch);
                        diffContentBuilder.Clear();
                    }

                    var parts = line.Split(' ');
                    if (parts.Length >= 3)
                    {
                        var baseFilePath = parts[2].Substring(2); // Remove the "a/" prefix
                        currentPatch = new FilePatch { BaseFilePath = baseFilePath };
                    }
                }
                else if (line.StartsWith("index") && currentPatch != null)
                {
                    // Extract the base index hash from the "index" line
                    var parts = line.Split(' ');
                    if (parts.Length >= 2)
                    {
                        var hashes = parts[1].Split("..");
                        if (hashes.Length > 0)
                        {
                            currentPatch.BaseIndex = hashes[0]; // The first hash is the base index
                        }
                    }
                }
                else if (currentPatch != null)
                {
                    if (line.StartsWith("---") || line.StartsWith("+++"))
                    {
                        // Skip the file header lines
                        continue;
                    }

                    // Add the line to the diff content
                    diffContentBuilder.AppendLine(line);
                }
            }

            // Add the last patch
            if (currentPatch != null)
            {
                currentPatch.DiffContent = diffContentBuilder.ToString();
                patches.Add(currentPatch);
            }

            return patches;
        }
    }

    internal static class PatchApplier
    {
        public static string Apply(string baseContent, string diffContent)
        {
            // Split the diff content into lines
            var diffLines = diffContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var baseLines = baseContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

            // Queue to store modifications (additions and removals)
            var modifications = new Dictionary<int, List<string>>();
            var currentIndex = 0;

            foreach (var line in diffLines)
            {
                if (line.StartsWith("@@")) // Hunk header
                {
                    // Parse the hunk header to get the line numbers
                    var hunkInfo = ParseHunkHeader(line);
                    currentIndex = hunkInfo.BaseStartLine - 1; // Convert to zero-based index
                }
                else if (line.StartsWith("-")) // Line to remove
                {
                    // Queue the removal of the line
                    if (!modifications.ContainsKey(currentIndex))
                    {
                        modifications[currentIndex] = new List<string>();
                    }
                    modifications[currentIndex].Add(null); // Null indicates a removal
                    currentIndex++;
                }
                else if (line.StartsWith("+")) // Line to add
                {
                    // Queue the addition of the line
                    if (!modifications.ContainsKey(currentIndex))
                    {
                        modifications[currentIndex] = new List<string>();
                    }
                    modifications[currentIndex].Add(line.Substring(1)); // Add the new line
                }
                else // Context line
                {
                    // Move to the next line in the base file
                    currentIndex++;
                }
            }

            // Apply modifications in a single pass
            var patchedLines = new List<string>();
            currentIndex = 0;

            for (var i = 0; i < baseLines.Count || modifications.ContainsKey(currentIndex); i++)
            {
                // Apply queued modifications at the current index
                if (modifications.ContainsKey(currentIndex))
                {
                    foreach (var modification in modifications[currentIndex])
                    {
                        if (modification != null)
                        {
                            patchedLines.Add(modification); // Add new line
                        }
                    }
                }

                // Add the current line from the base file if it hasn't been removed
                if (i < baseLines.Count && (!modifications.ContainsKey(currentIndex) || !modifications[currentIndex].Contains(null)))
                {
                    patchedLines.Add(baseLines[i]);
                }

                // Clear modifications for this index after processing
                if (modifications.ContainsKey(currentIndex))
                {
                    modifications.Remove(currentIndex);
                }

                currentIndex++;
            }

            return string.Join(Environment.NewLine, patchedLines);
        }

        private static (int BaseStartLine, int BaseLineCount, int TargetStartLine, int TargetLineCount) ParseHunkHeader(string hunkHeader)
        {
            // Example hunk header: @@ -3,7 +3,8 @@
            var parts = hunkHeader.Split(' ');
            var baseInfo = parts[1][1..].Split(',');
            var targetInfo = parts[2][1..].Split(',');

            var baseStartLine = int.Parse(baseInfo[0]);
            var baseLineCount = baseInfo.Length > 1 ? int.Parse(baseInfo[1]) : 1;

            var targetStartLine = int.Parse(targetInfo[0]);
            var targetLineCount = targetInfo.Length > 1 ? int.Parse(targetInfo[1]) : 1;

            return (baseStartLine, baseLineCount, targetStartLine, targetLineCount);
        }
    }

    internal class FilePatch
    {
        public string BaseFilePath { get; init; } = string.Empty;
        public string BaseIndex { get; set; } = string.Empty; // Add this to store the base index hash
        public string DiffContent { get; set; } = string.Empty;
    }
}