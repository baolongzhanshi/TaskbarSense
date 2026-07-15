using System.IO;
using System.Text.Json;
using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    public class LocalSettingsStore : ISettingsStore
    {
        private readonly string _filePath;
        private Dictionary<string, JsonElement> _cache = new();

        public LocalSettingsStore()
            : this(GetDefaultFilePath())
        {
        }

        public LocalSettingsStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Settings file path is required.", nameof(filePath));

            _filePath = filePath;
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            Load();
        }

        public static string GetDefaultFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "SmartTaskbar.Win11", "settings.json");
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
            {
                _cache = new Dictionary<string, JsonElement>();
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
                _cache = new Dictionary<string, JsonElement>();
                foreach (var property in document.RootElement.EnumerateObject())
                    _cache[property.Name] = property.Value.Clone();
            }
            catch
            {
                _cache = new Dictionary<string, JsonElement>();
            }
        }

        private void Save()
        {
            try
            {
                var dict = new Dictionary<string, object?>();
                foreach (var pair in _cache)
                    dict[pair.Key] = JsonSerializer.Deserialize<object>(pair.Value.GetRawText());

                var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                // Atomic-ish write: temp file then replace, so a crash mid-write won't wipe settings.
                var tempPath = _filePath + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(_filePath))
                    File.Replace(tempPath, _filePath, null);
                else
                    File.Move(tempPath, _filePath);
            }
            catch
            {
                // Keep in-memory cache; do not throw into UI thread on disk errors.
                try
                {
                    var tempPath = _filePath + ".tmp";
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // ignore cleanup failure
                }
            }
        }

        public T? GetValue<T>(string key)
        {
            if (!_cache.TryGetValue(key, out var element))
                return default;

            try
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (targetType == typeof(string))
                {
                    if (element.ValueKind == JsonValueKind.String)
                        return (T?)(object?)element.GetString();
                    return default;
                }

                if (targetType == typeof(bool))
                {
                    if (element.ValueKind == JsonValueKind.True)
                        return (T?)(object)true;
                    if (element.ValueKind == JsonValueKind.False)
                        return (T?)(object)false;
                    return default;
                }

                if (targetType == typeof(int))
                {
                    if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
                        return (T?)(object)number;
                    return default;
                }

                return element.Deserialize<T>();
            }
            catch
            {
                return default;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            _cache[key] = JsonSerializer.SerializeToElement(value);
            Save();
        }
    }
}