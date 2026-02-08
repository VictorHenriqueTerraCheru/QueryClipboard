using System;
using System.IO;
using Newtonsoft.Json;
using QueryClipboard.Models;

namespace QueryClipboard.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _settings;

        public SettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QueryClipboard"
            );
            
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");
            _settings = LoadSettings();
        }

        public AppSettings GetSettings()
        {
            return _settings;
        }

        public void SaveSettings(AppSettings settings)
        {
            _settings = settings;
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? CreateDefaultSettings();
                }
            }
            catch
            {
                // Se houver erro ao carregar, usa configurações padrão
            }

            return CreateDefaultSettings();
        }

        private AppSettings CreateDefaultSettings()
        {
            var settings = new AppSettings
            {
                HotkeyModifier = "Control+Alt",
                HotkeyKey = "Q",
                StorageMode = StorageMode.Json,
                Categories = new()
                {
                    new Category { Name = "DBA", Color = "#2196F3" },
                    new Category { Name = "Dev", Color = "#4CAF50" },
                    new Category { Name = "Reports", Color = "#FF9800" },
                    new Category { Name = "Queries", Color = "#9C27B0" }
                }
            };

            SaveSettings(settings);
            return settings;
        }
    }
}
