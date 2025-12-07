using System;
using System.IO;
using System.Text.Json;

namespace ScreenshotTool
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;
        private readonly string _appRootFolder;

        public SettingsService(string appRootFolder)
        {
            _appRootFolder = appRootFolder;
            _settingsPath = Path.Combine(appRootFolder, "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                        return loaded;
                }
            }
            catch
            {
                // Fallback to defaults
            }
            return new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(_appRootFolder);
                var json = JsonSerializer.Serialize(settings,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // ignore or log
            }
        }
    }
}
