using System.Text.Json;

namespace EyePatch
{
    public class Settings
    {
        public string DiffApp { get; init; } = "windiff";
        public string? PatchDirectory { get; init; }

        private static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eyepatch.settings");

        public static Settings Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }

            return new Settings();
        }

        public void Save()
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
    }
}
