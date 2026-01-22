using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pointless;

public class Settings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pointless",
        "settings.json"
    );

    public int IdleTimeoutSeconds { get; set; } = 3;
    public bool IgnoreKeyboardActivity { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public bool Enabled { get; set; } = true;

    // Hotkey settings
    public bool HotkeyEnabled { get; set; } = true;
    public Keys HotkeyKey { get; set; } = Keys.P;
    public bool HotkeyCtrl { get; set; } = true;
    public bool HotkeyAlt { get; set; } = true;
    public bool HotkeyShift { get; set; } = false;
    public bool HotkeyWin { get; set; } = false;

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize(json, SettingsContext.Default.Settings) ?? new Settings();
            }
        }
        catch
        {
            // Fall through to return default settings
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, SettingsContext.Default.Settings);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail - settings won't persist but app continues to work
        }
    }

    public string GetHotkeyDisplayString()
    {
        var parts = new List<string>();
        if (HotkeyCtrl) parts.Add("Ctrl");
        if (HotkeyAlt) parts.Add("Alt");
        if (HotkeyShift) parts.Add("Shift");
        if (HotkeyWin) parts.Add("Win");
        parts.Add(HotkeyKey.ToString());
        return string.Join(" + ", parts);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
internal partial class SettingsContext : JsonSerializerContext
{
}
