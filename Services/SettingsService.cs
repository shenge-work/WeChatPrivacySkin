using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeChatPrivacySkin;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object _gate = new();

    public event EventHandler<AppSettings>? SettingsChanged;

    public AppSettings Current { get; private set; } = new();

    public string SettingsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WeChatPrivacySkin");

    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public AppSettings Load()
    {
        lock (_gate)
        {
            Directory.CreateDirectory(SettingsDirectory);

            if (!File.Exists(SettingsPath))
            {
                Current = new AppSettings();
                Save();
                return Current;
            }

            try
            {
                var json = File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
                Normalize();
            }
            catch
            {
                Current = new AppSettings();
                Save();
            }

            return Current;
        }
    }

    public void Update(Action<AppSettings> update)
    {
        lock (_gate)
        {
            update(Current);
            Normalize();
            Save();
        }

        SettingsChanged?.Invoke(this, Current);
    }

    public void Save()
    {
        lock (_gate)
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(Current, _jsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
    }

    private void Normalize()
    {
        Current.OverlayOpacity = Math.Clamp(Current.OverlayOpacity, 0.35, 0.95);
        if (string.IsNullOrWhiteSpace(Current.WeChatExecutablePath))
        {
            Current.WeChatExecutablePath = new AppSettings().WeChatExecutablePath;
        }

        if (!string.IsNullOrWhiteSpace(Current.BackgroundImagePath) &&
            !File.Exists(Current.BackgroundImagePath))
        {
            Current.BackgroundImagePath = null;
        }
    }
}
