using AiQuotaMonitor.App.Services;

namespace AiQuotaMonitor.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void SettingsSaveAtomicallyAndContainOnlyUserPreferences()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AiQuotaMonitor-tests", Guid.NewGuid().ToString("N"));
        var settingsPath = Path.Combine(directory, "settings.json");
        var store = new SettingsStore(settingsPath);

        store.Save(new AppSettings
        {
            RefreshIntervalSeconds = 900,
            Theme = ThemeMode.Light,
            ShowUsedPercent = true,
            LowAlertsEnabled = true,
            AlertThresholdPercent = 10,
            FloatingAlwaysOnTop = false
        });

        var json = File.ReadAllText(settingsPath);
        var loaded = store.Load();
        Assert.Equal(900, loaded.RefreshIntervalSeconds);
        Assert.Equal(ThemeMode.Light, loaded.Theme);
        Assert.True(loaded.LowAlertsEnabled);
        Assert.Equal(10, loaded.AlertThresholdPercent);
        Assert.False(loaded.FloatingAlwaysOnTop);
        Assert.False(json.Contains("token", StringComparison.OrdinalIgnoreCase));
        Assert.False(json.Contains("credential", StringComparison.OrdinalIgnoreCase));
        Assert.False(File.Exists(settingsPath + ".tmp"));

        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void CorruptAndUnknownSchemaSettingsFallBackToDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AiQuotaMonitor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var settingsPath = Path.Combine(directory, "settings.json");
        var store = new SettingsStore(settingsPath);

        File.WriteAllText(settingsPath, "not-json");
        Assert.Equal(300, store.Load().RefreshIntervalSeconds);

        File.WriteAllText(settingsPath, "{\"schemaVersion\":2,\"refreshIntervalSeconds\":60}");
        Assert.Equal(300, store.Load().RefreshIntervalSeconds);

        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void InvalidPreferenceValuesAreClampedOrRejectedToSafeDefaults()
    {
        var settings = new AppSettings
        {
            RefreshIntervalSeconds = 5,
            AlertThresholdPercent = 77,
            Theme = (ThemeMode)99
        }.Normalize();

        Assert.Equal(60, settings.RefreshIntervalSeconds);
        Assert.Equal(20, settings.AlertThresholdPercent);
        Assert.Equal(ThemeMode.Dark, settings.Theme);
    }
}
