using System.Text.Json.Nodes;
using AiQuotaMonitor.App.Services;

namespace AiQuotaMonitor.Tests;

public sealed class ClaudeStatuslineSetupServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AiQuotaMonitorSetupTests", Guid.NewGuid().ToString("N"));
    private readonly string _userProfile;
    private readonly string _localAppData;
    private readonly string _appData;

    public ClaudeStatuslineSetupServiceTests()
    {
        _userProfile = Path.Combine(_root, "user");
        _localAppData = Path.Combine(_root, "local");
        _appData = Path.Combine(_root, "roaming");
        Directory.CreateDirectory(_userProfile);
    }

    [Fact]
    public void ConfigureCreatesSettingsAndHelperThenRollbackRemovesOnlyItsOwnEntry()
    {
        var service = CreateService(claudeInstalled: true);
        var settingsPath = Path.Combine(_userProfile, ".claude", "settings.json");

        Assert.Equal("statusline_not_configured", service.Inspect().ErrorCode);
        var configured = service.Configure();

        Assert.Equal("configured", configured.ResultCode);
        Assert.True(File.Exists(settingsPath));
        var installedHelper = File.ReadAllBytes(Path.Combine(_localAppData, "AiQuotaMonitor", "claude-statusline.ps1"));
        Assert.True(installedHelper.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        var settings = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
        Assert.Equal("command", settings["statusLine"]?["type"]?.GetValue<string>());
        Assert.Contains("claude-statusline.ps1", settings["statusLine"]?["command"]?.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
        Assert.True(service.Inspect().CanRollback);
        Assert.Equal("statusline_waiting_for_data", service.Inspect().ErrorCode);

        var rolledBack = service.Rollback();

        Assert.Equal("restored", rolledBack.ResultCode);
        Assert.False(File.Exists(settingsPath));
        Assert.False(service.Inspect().CanRollback);
    }

    [Fact]
    public void MissingClaudeExecutableHasADistinctInitialState()
    {
        Assert.Equal("claude_cli_not_found", CreateService().Inspect().ErrorCode);
    }

    [Fact]
    public void MissingInstalledHelperIsReportedAndCanBeSafelyRolledBack()
    {
        var service = CreateService(claudeInstalled: true);
        Assert.Equal("configured", service.Configure().ResultCode);
        File.Delete(Path.Combine(_localAppData, "AiQuotaMonitor", "claude-statusline.ps1"));

        var state = service.Inspect();

        Assert.Equal("helper_unavailable", state.ErrorCode);
        Assert.True(state.CanRollback);
    }

    [Fact]
    public void ModifiedInstalledHelperIsReportedWithoutOverwritingIt()
    {
        var service = CreateService(claudeInstalled: true);
        Assert.Equal("configured", service.Configure().ResultCode);
        var helperPath = Path.Combine(_localAppData, "AiQuotaMonitor", "claude-statusline.ps1");
        File.WriteAllText(helperPath, "user modified helper");

        var state = service.Inspect();

        Assert.Equal("helper_file_conflict", state.ErrorCode);
        Assert.True(state.CanRollback);
        Assert.Equal("user modified helper", File.ReadAllText(helperPath));
    }

    [Fact]
    public void ConfigureBacksUpWholeExistingSettingsAndPreservesUnrelatedValues()
    {
        var settingsDirectory = Path.Combine(_userProfile, ".claude");
        Directory.CreateDirectory(settingsDirectory);
        var settingsPath = Path.Combine(settingsDirectory, "settings.json");
        var original = "{\"permissions\":{\"allow\":[\"Read(*)\"]},\"env\":{\"SYNTHETIC_ONLY\":\"redacted-value\"}}";
        File.WriteAllText(settingsPath, original);
        var service = CreateService();

        var result = service.Configure();

        Assert.Equal("configured", result.ResultCode);
        Assert.DoesNotContain("redacted-value", result.Message, StringComparison.Ordinal);
        var backup = Assert.Single(Directory.GetFiles(settingsDirectory, "settings.json.aiquota-*.bak"));
        Assert.Equal(original, File.ReadAllText(backup));
        var updated = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
        Assert.Equal("redacted-value", updated["env"]?["SYNTHETIC_ONLY"]?.GetValue<string>());
        Assert.NotNull(updated["permissions"]);
        Assert.NotNull(updated["statusLine"]);
    }

    [Fact]
    public void ExistingStatuslineIsAConflictAndIsNeverOverwritten()
    {
        var settingsDirectory = Path.Combine(_userProfile, ".claude");
        Directory.CreateDirectory(settingsDirectory);
        var settingsPath = Path.Combine(settingsDirectory, "settings.json");
        var original = "{\"statusLine\":{\"type\":\"command\",\"command\":\"existing-user-statusline\"},\"theme\":\"dark\"}";
        File.WriteAllText(settingsPath, original);
        var service = CreateService();

        var result = service.Configure();

        Assert.Equal("existing_statusline_conflict", result.ResultCode);
        Assert.Equal(original, File.ReadAllText(settingsPath));
        Assert.Empty(Directory.GetFiles(settingsDirectory, "settings.json.aiquota-*.bak"));
    }

    [Fact]
    public void MalformedSettingsAreNotRewritten()
    {
        var settingsDirectory = Path.Combine(_userProfile, ".claude");
        Directory.CreateDirectory(settingsDirectory);
        var settingsPath = Path.Combine(settingsDirectory, "settings.json");
        const string original = "{ malformed";
        File.WriteAllText(settingsPath, original);
        var service = CreateService();

        var result = service.Configure();

        Assert.Equal("settings_parse_failure", result.ResultCode);
        Assert.Equal(original, File.ReadAllText(settingsPath));
    }

    [Fact]
    public void RollbackRefusesToOverwriteAStatuslineChangedByTheUser()
    {
        var service = CreateService();
        var settingsPath = Path.Combine(_userProfile, ".claude", "settings.json");
        Assert.Equal("configured", service.Configure().ResultCode);
        var current = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
        current["statusLine"]!["command"] = "user-edited-statusline";
        var edited = current.ToJsonString();
        File.WriteAllText(settingsPath, edited);

        var result = service.Rollback();

        Assert.Equal("settings_changed_since_setup", result.ResultCode);
        Assert.Equal(edited, File.ReadAllText(settingsPath));
    }

    [Fact]
    public void RollbackRefusesChangesToTheStatuslineObjectEvenWhenItsCommandIsUnchanged()
    {
        var service = CreateService(claudeInstalled: true);
        var settingsPath = Path.Combine(_userProfile, ".claude", "settings.json");
        Assert.Equal("configured", service.Configure().ResultCode);
        var current = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
        current["statusLine"]!["padding"] = "user-added";
        var edited = current.ToJsonString();
        File.WriteAllText(settingsPath, edited);

        var result = service.Rollback();

        Assert.Equal("settings_changed_since_setup", result.ResultCode);
        Assert.Equal(edited, File.ReadAllText(settingsPath));
    }

    [Fact]
    public void RollbackPreservesSettingsAddedAfterTheMonitorSetup()
    {
        var settingsDirectory = Path.Combine(_userProfile, ".claude");
        Directory.CreateDirectory(settingsDirectory);
        var settingsPath = Path.Combine(settingsDirectory, "settings.json");
        File.WriteAllText(settingsPath, "{\"theme\":\"dark\"}");
        var service = CreateService();
        Assert.Equal("configured", service.Configure().ResultCode);
        var current = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
        current["extraSetting"] = "kept";
        File.WriteAllText(settingsPath, current.ToJsonString());

        var result = service.Rollback();

        Assert.Equal("restored", result.ResultCode);
        var restored = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
        Assert.Equal("dark", restored["theme"]?.GetValue<string>());
        Assert.Equal("kept", restored["extraSetting"]?.GetValue<string>());
        Assert.Null(restored["statusLine"]);
    }

    private ClaudeStatuslineSetupService CreateService(bool claudeInstalled = false) => new(
        userProfile: _userProfile,
        localAppData: _localAppData,
        appData: _appData,
        pathVariable: null,
        helperContent: "# Synthetic helper contents; not a credential.",
        fileExists: path => Path.GetFileName(path).Equals("claude-statusline.ps1", StringComparison.OrdinalIgnoreCase)
            ? File.Exists(path)
            : claudeInstalled);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
