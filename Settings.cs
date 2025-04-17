using System.Text.Json;

namespace EyePatch
{
    public class Settings
    {
        public string? DiffApp { get; set; }
        public string? PatchDirectory { get; init; }

        private static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eyepatch.settings");

        public static Settings Load()
        {
            Settings settings;

            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
            else
            {
                settings = new Settings();
            }

            settings.DiffApp ??= "windiff";

            return settings;
        }
    }
}
