using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeChatPrivacySkin;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
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
                Save();
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
        if (Current.PrivacyEnabled.HasValue)
        {
            Current.Privacy.Enabled = Current.PrivacyEnabled.Value;
            Current.PrivacyEnabled = null;
        }

        if (Current.Theme.HasValue)
        {
            Current.ThemePackId = ThemeCatalog.MapLegacy(Current.Theme.Value);
            Current.Theme = null;
        }

        Current.Privacy ??= new PrivacyProfile();
        if (!Enum.IsDefined(Current.Privacy.Mode))
        {
            Current.Privacy.Mode = PrivacyMode.DailyProtection;
        }

        if (string.IsNullOrWhiteSpace(Current.ThemePackId) || !ThemeCatalog.Contains(Current.ThemePackId))
        {
            Current.ThemePackId = ThemeCatalog.DefaultThemeId;
        }

        if (string.IsNullOrWhiteSpace(Current.OverlaySkinId) || !OverlaySkinCatalog.Contains(Current.OverlaySkinId))
        {
            Current.OverlaySkinId = OverlaySkinCatalog.DefaultSkinId;
        }

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

        if (!IsValidCustomSkinPath(Current.CustomSkinImagePath))
        {
            Current.CustomSkinImagePath = null;
        }

        if (string.Equals(Current.OverlaySkinId, OverlaySkinCatalog.CustomPngSkinId, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(Current.CustomSkinImagePath))
        {
            Current.OverlaySkinId = OverlaySkinCatalog.DefaultSkinId;
        }

        Current.WindowRules ??= [];
        MergeDefaultWindowRules();
    }

    private static bool IsValidCustomSkinPath(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               File.Exists(path) &&
               string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase);
    }

    private void MergeDefaultWindowRules()
    {
        foreach (var defaultRule in WindowRule.Defaults())
        {
            if (Current.WindowRules.Any(rule => string.Equals(rule.Id, defaultRule.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            Current.WindowRules.Add(defaultRule);
        }
    }
}
