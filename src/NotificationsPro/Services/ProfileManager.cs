using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationsPro.Helpers;
using NotificationsPro.Models;

namespace NotificationsPro.Services;

/// <summary>
/// Manages named settings profiles stored in %AppData%\NotificationsPro\profiles\.
/// Each profile is a complete AppSettings snapshot saved as a JSON file.
/// Never stores notification content.
/// </summary>
public class ProfileManager
{
    private readonly string _profilesDir;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ProfileManager() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotificationsPro", "profiles"))
    { }

    public ProfileManager(string profilesDir)
    {
        _profilesDir = profilesDir;
    }

    public List<string> GetProfileNames()
    {
        if (!Directory.Exists(_profilesDir))
            return new List<string>();

        return Directory.GetFiles(_profilesDir, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void SaveProfile(string name, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        Directory.CreateDirectory(_profilesDir);
        var filename = SanitizeFileName(name) + ".json";
        var path = Path.Combine(_profilesDir, filename);
        var snapshot = settings.Clone();
        if (!snapshot.SettingsSchemaVersion.HasValue || snapshot.SettingsSchemaVersion.Value < SettingsManager.CurrentSettingsSchemaVersion)
            snapshot.SettingsSchemaVersion = SettingsManager.CurrentSettingsSchemaVersion;
        snapshot = AppSettingsAssetPathHelper.CreatePortableSnapshot(snapshot);
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        File.WriteAllText(path, json);
    }

    public AppSettings? LoadProfile(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !Directory.Exists(_profilesDir))
            return null;

        var filename = SanitizeFileName(name) + ".json";
        var path = Path.Combine(_profilesDir, filename);
        if (!File.Exists(path))
            return null;

        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > SettingsManager.MaxSettingsFileBytes)
                return null;

            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (loaded != null)
                AppSettingsAssetPathHelper.NormalizeForRuntime(loaded);

            return loaded;
        }
        catch
        {
            return null;
        }
    }

    public void DeleteProfile(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !Directory.Exists(_profilesDir))
            return;

        var filename = SanitizeFileName(name) + ".json";
        var path = Path.Combine(_profilesDir, filename);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
