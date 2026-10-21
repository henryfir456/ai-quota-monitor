using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiQuotaMonitor.Providers.Claude;

namespace AiQuotaMonitor.App.Services;

public sealed record ClaudeStatuslineSetupState(
    string ErrorCode,
    string ButtonText,
    bool CanRollback,
    bool HasExistingStatusline);

public sealed record ClaudeStatuslineSetupResult(string ResultCode, string Message);

public sealed class ClaudeStatuslineSetupService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions JournalOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string _userProfile;
    private readonly string _localApplicationData;
    private readonly string _monitorDirectory;
    private readonly string? _appData;
    private readonly string? _pathVariable;
    private readonly string? _helperContent;
    private readonly Func<string, bool> _fileExists;

    public ClaudeStatuslineSetupService(
        string? userProfile = null,
        string? localAppData = null,
        string? appData = null,
        string? pathVariable = null,
        string? helperContent = null,
        Func<string, bool>? fileExists = null)
    {
        _userProfile = userProfile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _localApplicationData = localAppData ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _monitorDirectory = Path.Combine(_localApplicationData, "AiQuotaMonitor");
        _appData = appData ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _pathVariable = pathVariable ?? Environment.GetEnvironmentVariable("PATH");
        _helperContent = helperContent ?? ReadEmbeddedHelper();
        _fileExists = fileExists ?? File.Exists;
    }

    public string SettingsPath => Path.Combine(_userProfile, ".claude", "settings.json");

    public ClaudeStatuslineSetupState Inspect()
    {
        JsonObject settings;
        try
        {
            settings = ReadSettings(out _);
        }
        catch (JsonException)
        {
            return new("settings_parse_failure", "檢查 Claude 設定", false, true);
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
            return new("settings_read_failure", "檢查 Claude 設定", false, false);
        }

        if (!settings.ContainsKey("statusLine") && TryReadJournal(out _))
        {
            return new("statusline_setup_incomplete", "復原未完成的 Claude 設定", true, false);
        }

        if (!settings.ContainsKey("statusLine") && JournalPathExists())
        {
            return new("setup_journal_unreadable", "檢查 Claude 設定記錄", false, false);
        }

        if (settings.TryGetPropertyValue("statusLine", out var statusLine))
        {
            var command = ReadCommand(statusLine);
            if (command is null)
            {
                return new("statusline_configuration_conflict", "檢查 Claude 設定", false, true);
            }

            if (TryReadJournal(out var journal) &&
                MatchesInstalledStatusLine(statusLine, journal.InstalledCommand))
            {
                var code = ClaudeExecutableLocator.Find(_pathVariable, _localApplicationData, _appData, _fileExists, _userProfile) is null
                    ? "claude_cli_not_found"
                    : InspectInstalledHelperErrorCode() ?? "statusline_waiting_for_data";
                return new(code, "還原 Claude 額度設定", true, true);
            }

            if (MatchesInstalledStatusLine(statusLine, CreateCommand()))
            {
                var code = ClaudeExecutableLocator.Find(_pathVariable, _localApplicationData, _appData, _fileExists, _userProfile) is null
                    ? "claude_cli_not_found"
                    : InspectInstalledHelperErrorCode() ?? "statusline_waiting_for_data";
                return new(code, "檢查 Claude 額度來源", false, true);
            }

            return new("statusline_configuration_conflict", "檢查 Claude 設定", false, true);
        }

        var notInstalled = ClaudeExecutableLocator.Find(_pathVariable, _localApplicationData, _appData, _fileExists, _userProfile) is null;
        return notInstalled
            ? new("claude_cli_not_found", "設定 Claude 額度來源", false, false)
            : new("statusline_not_configured", "設定 Claude 額度來源", false, false);
    }

    public ClaudeStatuslineSetupResult Configure()
    {
        byte[]? originalBytes = null;
        var settingsExisted = false;
        JsonObject settings;
        try
        {
            settingsExisted = TryReadFileIfPresent(SettingsPath, out var currentSettingsBytes);
            if (settingsExisted)
            {
                originalBytes = currentSettingsBytes;
                settings = ParseSettings(currentSettingsBytes);
            }
            else
            {
                settings = new JsonObject();
            }
        }
        catch (JsonException)
        {
            return new("settings_parse_failure", "Claude settings.json 格式無法解析；檔案未修改。請先用 Claude Code 或文字編輯器修復格式，再重試。");
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
            return new("settings_read_failure", "無法安全讀取 Claude settings.json；檔案未修改。請檢查檔案權限後重試。");
        }

        if (!settings.ContainsKey("statusLine") && TryReadJournal(out _))
        {
            return new("statusline_setup_incomplete", "已有未完成的 Claude 額度設定記錄。請先復原或檢查該設定，再重新設定。");
        }

        if (!settings.ContainsKey("statusLine") && JournalPathExists())
        {
            return new("setup_journal_unreadable", "Claude 額度設定記錄無法安全讀取；為避免覆寫，Monitor 未修改設定。");
        }

        if (settings.ContainsKey("statusLine"))
        {
            var current = ReadCommand(settings["statusLine"]);
            if (current is not null && TryReadJournal(out var journal) &&
                MatchesInstalledStatusLine(settings["statusLine"], journal.InstalledCommand))
            {
                return new("already_configured", "Claude 額度來源已由 Monitor 設定；Claude Code 下一次傳送支援的 rate_limits 欄位後，主畫面會更新。");
            }

            return new("existing_statusline_conflict", "偵測到既有 Claude statusLine 設定。為保留你的自訂指令，Monitor 未修改 settings.json。請自行整合 helper，或先備份並調整原設定後再試。");
        }

        if (string.IsNullOrWhiteSpace(_helperContent))
        {
            return new("helper_unavailable", "Monitor 內嵌的 Claude statusline helper 無法讀取；Claude 設定未修改。請重新下載完整 portable ZIP。");
        }

        var helperPath = Path.Combine(_monitorDirectory, "claude-statusline.ps1");
        var helperBytes = EncodeHelper(_helperContent);
        var helperCreated = false;
        string? backupPath = null;
        var command = CreateCommand(helperPath);
        byte[]? updatedSettings = null;
        var settingsCommitted = false;
        byte[]? journalBytes = null;
        var journalWritten = false;

        try
        {
            if (File.Exists(helperPath))
            {
                var existingHelper = File.ReadAllBytes(helperPath);
                if (!existingHelper.AsSpan().SequenceEqual(helperBytes))
                {
                    return new("helper_file_conflict", "Monitor 的 Claude helper 路徑已存在其他內容。為避免覆寫使用者檔案，未修改 Claude 設定。");
                }
            }

            Directory.CreateDirectory(_monitorDirectory);
            if (!File.Exists(helperPath))
            {
                WriteNewFile(helperPath, helperBytes);
                helperCreated = true;
            }

            if (settingsExisted)
            {
                EnsureUnchanged(SettingsPath, originalBytes!);
                backupPath = CreateBackup(originalBytes!);
            }
            else if (TryReadFileIfPresent(SettingsPath, out _))
            {
                if (helperCreated)
                {
                    DeleteIfMatches(helperPath, helperBytes);
                }

                return new("settings_changed_during_setup", "Claude settings.json 在設定期間被其他程式建立。為避免覆蓋，Monitor 未修改該檔案；請重新檢查後再試。");
            }

            settings["statusLine"] = new JsonObject
            {
                ["type"] = "command",
                ["command"] = command
            };
            updatedSettings = new UTF8Encoding(false).GetBytes(settings.ToJsonString(JsonOptions));
            if (settingsExisted)
            {
                EnsureUnchanged(SettingsPath, originalBytes!);
            }
            else if (TryReadFileIfPresent(SettingsPath, out _))
            {
                if (helperCreated)
                {
                    DeleteIfMatches(helperPath, helperBytes);
                }

                return new("settings_changed_during_setup", "Claude settings.json 在設定期間被其他程式建立。為避免覆蓋，Monitor 未修改該檔案；請重新檢查後再試。");
            }

            var journal = new SetupJournal(
                settingsExisted,
                backupPath,
                command,
                originalBytes is null ? null : Convert.ToHexString(SHA256.HashData(originalBytes)));
            journalBytes = JsonSerializer.SerializeToUtf8Bytes(journal, JsonOptions);
            WriteAtomicNew(JournalPath, journalBytes);
            journalWritten = true;

            if (settingsExisted)
            {
                WriteAtomic(SettingsPath, updatedSettings);
            }
            else
            {
                WriteAtomicNew(SettingsPath, updatedSettings);
            }
            settingsCommitted = true;
            return new("configured", settingsExisted
                ? "Claude Code 官方 statusline 已設定，原 settings.json 已建立本機備份。只有 Claude Code 提供 rate_limits 欄位時才會顯示額度；可用同一按鈕復原設定。"
                : "Claude Code 官方 statusline 已設定。沒有既有設定需要備份；只有 Claude Code 提供 rate_limits 欄位時才會顯示額度。可用同一按鈕復原設定。");
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
            if (settingsCommitted && updatedSettings is not null)
            {
                RestoreIfUnchanged(SettingsPath, originalBytes, settingsExisted, updatedSettings);
            }

            if (helperCreated)
            {
                DeleteIfMatches(helperPath, helperBytes);
            }

            if (journalWritten && journalBytes is not null)
            {
                DeleteIfMatches(JournalPath, journalBytes);
            }

            return new("settings_write_failure", "Claude statusline 設定無法安全寫入；Monitor 已嘗試保留原檔。請檢查檔案權限，必要時使用設定旁的備份後再重試。");
        }
    }

    public ClaudeStatuslineSetupResult Rollback()
    {
        if (!TryReadJournal(out var journal))
        {
            if (JournalPathExists())
            {
                return new("setup_journal_unreadable", "Claude 額度設定記錄無法安全讀取；Monitor 未修改設定。");
            }

            return new("rollback_not_available", "找不到由 Monitor 建立的 Claude 設定記錄；沒有變更設定。");
        }

        byte[] currentBytes;
        JsonObject settings;
        try
        {
            if (!TryReadFileIfPresent(SettingsPath, out currentBytes))
            {
                if (journal.OriginalSettingsExisted)
                {
                    return new("settings_changed_since_setup", "原有 Claude settings.json 已不存在；Monitor 保留本機備份且未自行還原或覆寫。");
                }

                return CleanupInterruptedSetup(journal);
            }

            settings = ParseSettings(currentBytes);
        }
        catch (JsonException)
        {
            return new("settings_parse_failure", "Claude settings.json 格式已變更且無法解析；為避免覆寫，Monitor 保留備份並停止復原。");
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
            return new("settings_read_failure", "無法安全讀取 Claude settings.json；Monitor 保留備份且未修改設定。");
        }

        if (!MatchesInstalledStatusLine(settings["statusLine"], journal.InstalledCommand))
        {
            if (journal.OriginalSettingsExisted &&
                journal.OriginalSettingsSha256 is not null &&
                string.Equals(
                    Convert.ToHexString(SHA256.HashData(currentBytes)),
                    journal.OriginalSettingsSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                return CleanupInterruptedSetup(journal);
            }

            return new("settings_changed_since_setup", "statusLine 已由你或其他程式修改。為保留新設定，Monitor 未復原或覆寫任何內容。");
        }

        settings.Remove("statusLine");
        try
        {
            EnsureUnchanged(SettingsPath, currentBytes);
            if (!journal.OriginalSettingsExisted && settings.Count == 0)
            {
                File.Delete(SettingsPath);
            }
            else
            {
                var restoredBytes = new UTF8Encoding(false).GetBytes(settings.ToJsonString(JsonOptions));
                WriteAtomic(SettingsPath, restoredBytes);
            }

            DeleteIfMatches(JournalPath, SerializeJournal(journal));
            return new("restored", "Monitor 加入的 statusLine 已移除；其他設定保留。本機備份檔仍保留，helper 檔也未刪除。");
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
            return new("settings_write_failure", "Claude 設定無法安全復原；原設定與備份均保留，沒有嘗試覆寫其他內容。");
        }
    }

    private ClaudeStatuslineSetupResult CleanupInterruptedSetup(SetupJournal journal)
    {
        DeleteIfMatches(JournalPath, SerializeJournal(journal));
        var helperPath = Path.Combine(_monitorDirectory, "claude-statusline.ps1");
        if (!string.IsNullOrWhiteSpace(_helperContent))
        {
            DeleteIfMatches(helperPath, EncodeHelper(_helperContent));
        }

        return new("restored", "Claude 額度設定尚未完整寫入；Monitor 已清除自己的復原記錄與 helper，沒有修改其他設定。");
    }

    private string JournalPath => Path.Combine(_monitorDirectory, "claude-statusline-setup.json");

    private string CreateCommand() => CreateCommand(Path.Combine(_monitorDirectory, "claude-statusline.ps1"));

    private string? InspectInstalledHelperErrorCode()
    {
        if (string.IsNullOrWhiteSpace(_helperContent))
        {
            return "helper_unavailable";
        }

        var helperPath = Path.Combine(_monitorDirectory, "claude-statusline.ps1");
        if (!_fileExists(helperPath))
        {
            return "helper_unavailable";
        }

        try
        {
            var expectedBytes = EncodeHelper(_helperContent);
            return File.ReadAllBytes(helperPath).AsSpan().SequenceEqual(expectedBytes)
                ? null
                : "helper_file_conflict";
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
            return "helper_read_failure";
        }
    }

    private static string CreateCommand(string helperPath) =>
        $"powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File \"{helperPath}\"";

    private static byte[] EncodeHelper(string content)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(content)).ToArray();
    }

    private string CreateBackup(byte[] originalBytes)
    {
        var directory = Path.GetDirectoryName(SettingsPath) ?? throw new IOException("Invalid settings path.");
        var path = SettingsPath + ".aiquota-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture) + ".bak";
        Directory.CreateDirectory(directory);
        WriteNewFile(path, originalBytes);
        return path;
    }

    private JsonObject ReadSettings(out byte[]? bytes)
    {
        if (!TryReadFileIfPresent(SettingsPath, out var settingsBytes))
        {
            bytes = null;
            return new JsonObject();
        }

        bytes = settingsBytes;
        return ParseSettings(settingsBytes);
    }

    private static JsonObject ParseSettings(byte[] bytes)
    {
        var text = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
        using var document = JsonDocument.Parse(text);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            document.RootElement.EnumerateObject().Count(property => property.NameEquals("statusLine")) > 1)
        {
            throw new JsonException("Claude settings must have a single object root and at most one statusLine entry.");
        }

        var node = JsonNode.Parse(text);
        return node as JsonObject ?? throw new JsonException("Settings root must be an object.");
    }

    private bool TryReadJournal(out SetupJournal journal)
    {
        try
        {
            if (!TryReadFileIfPresent(JournalPath, out var journalBytes))
            {
                journal = new SetupJournal(false, null, string.Empty, null);
                return false;
            }

            var parsed = JsonSerializer.Deserialize<SetupJournal>(journalBytes, JournalOptions);
            if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.InstalledCommand))
            {
                journal = parsed;
                return true;
            }
        }
        catch (Exception exception) when (IsFileAccessFailure(exception) || exception is JsonException)
        {
        }

        journal = new SetupJournal(false, null, string.Empty, null);
        return false;
    }

    private bool JournalPathExists()
    {
        try
        {
            return TryReadFileIfPresent(JournalPath, out _);
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
            return true;
        }
    }

    private static string? ReadCommand(JsonNode? statusLine)
    {
        if (statusLine is not JsonObject configuration ||
            configuration["command"] is not JsonValue commandValue ||
            !commandValue.TryGetValue<string>(out var command))
        {
            return null;
        }

        return command;
    }

    private static bool MatchesInstalledStatusLine(JsonNode? statusLine, string expectedCommand)
    {
        return statusLine is JsonObject configuration &&
               configuration.Count == 2 &&
               configuration["type"] is JsonValue typeValue &&
               typeValue.TryGetValue<string>(out var type) &&
               string.Equals(type, "command", StringComparison.Ordinal) &&
               string.Equals(ReadCommand(configuration), expectedCommand, StringComparison.Ordinal);
    }

    private static void EnsureUnchanged(string path, byte[] expected)
    {
        if (!TryReadFileIfPresent(path, out var actual) || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new IOException("Settings changed during the update.");
        }
    }

    private static bool TryReadFileIfPresent(string path, out byte[] bytes)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            bytes = buffer.ToArray();
            return true;
        }
        catch (FileNotFoundException)
        {
            bytes = [];
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            bytes = [];
            return false;
        }
    }

    private static void WriteNewFile(string path, byte[] bytes)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    private static void WriteAtomic(string path, byte[] bytes)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new IOException("Invalid output path.");
        Directory.CreateDirectory(directory);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            WriteNewFile(temporaryPath, bytes);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void WriteAtomicNew(string path, byte[] bytes)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new IOException("Invalid output path.");
        Directory.CreateDirectory(directory);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            WriteNewFile(temporaryPath, bytes);
            File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static byte[] SerializeJournal(SetupJournal journal) =>
        JsonSerializer.SerializeToUtf8Bytes(journal, JsonOptions);

    private static void RestoreIfUnchanged(string path, byte[]? original, bool originalExisted, byte[] expected)
    {
        try
        {
            if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(expected))
            {
                return;
            }

            if (originalExisted && original is not null)
            {
                WriteAtomic(path, original);
            }
            else
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
        }
    }

    private static void DeleteIfMatches(string path, byte[] expected)
    {
        try
        {
            if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(expected))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (IsFileAccessFailure(exception))
        {
        }
    }

    private static bool IsFileAccessFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;

    private static string? ReadEmbeddedHelper()
    {
        var assembly = typeof(ClaudeStatuslineSetupService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("claude-statusline.ps1", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private sealed record SetupJournal(
        bool OriginalSettingsExisted,
        string? BackupPath,
        string InstalledCommand,
        string? OriginalSettingsSha256);
}
