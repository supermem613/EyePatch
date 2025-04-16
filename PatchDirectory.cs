namespace EyePatch;

internal static class PatchDirectory
{
    internal static string EnsurePatchesDirectoryExists(Settings settings)
    {
        var patchesPath = settings.PatchDirectory;

        if (string.IsNullOrEmpty(patchesPath))
        {
            // Fallback to OneDrive path from the environment variable
            var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");

            if (string.IsNullOrEmpty(oneDrivePath))
            {
                throw new InvalidOperationException("OneDrive is not configured on this system. Either install it or configure a patch directory.");
            }

            // Construct the full path to the "patches" directory
            patchesPath = Path.Combine(oneDrivePath, "patches");
        }

        // Ensure the "patches" directory exists
        if (!Directory.Exists(patchesPath))
        {
            Directory.CreateDirectory(patchesPath);
        }

        // Return the full path
        return patchesPath;
    }
}