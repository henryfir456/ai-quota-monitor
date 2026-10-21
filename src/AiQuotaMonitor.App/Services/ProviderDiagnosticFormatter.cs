using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.App.Services;

public sealed record ProviderDiagnostic(string Status, string Reason, string Recommendation)
{
    public string DiagnosticText => $"原因：{Reason} 建議：{Recommendation}";
}

public static class ProviderDiagnosticFormatter
{
    public static ProviderDiagnostic Describe(QuotaObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var status = GetStatus(observation);

        var description = observation.ErrorCode is null
            ? observation.Status switch
            {
                QuotaStatus.Fresh => ("來源已回報此額度視窗的合法百分比。", "等待來源下一次正常更新；Monitor 不會推測未回報欄位。"),
                QuotaStatus.Stale => ("最後成功資料已超過 15 分鐘。", "等待 Claude Code 下一次 statusline 更新，或重新整理 Codex 資料。"),
                QuotaStatus.Unsupported => ("來源尚未支援此額度視窗。", "查看來源版本與帳號方案；Monitor 不會用其他數值推算。"),
                _ => DescribeCause(observation.ProviderId, observation.ErrorCode)
            }
            : DescribeCause(observation.ProviderId, observation.ErrorCode);
        var (reason, recommendation) = description;
        return new ProviderDiagnostic(status, reason, recommendation);
    }

    private static string GetStatus(QuotaObservation observation) => observation.Status switch
    {
        QuotaStatus.Fresh when observation.ErrorCode == "invalid_reset_timestamp" => "資料最新（重置時間無效）",
        QuotaStatus.Fresh => "資料最新",
        QuotaStatus.Stale => "資料已過期",
        QuotaStatus.Unsupported when observation.ErrorCode == "codex_cli_unavailable" => "程式無法啟動",
        QuotaStatus.Unsupported => "未支援",
        QuotaStatus.Error when observation.ErrorCode == "parse_failure" => "資料解析失敗",
        QuotaStatus.Error when observation.ErrorCode == "timeout" => "查詢逾時",
        QuotaStatus.Error when observation.ErrorCode is "app_server_unreachable" or "network_unreachable" => "無法連線",
        QuotaStatus.Error => "資料查詢失敗",
        _ => observation.ErrorCode switch
        {
            "claude_cli_not_found" or "codex_cli_not_found" => "CLI 未安裝",
            "statusline_not_configured" => "尚未設定",
            "claude_settings_not_inspected" => "尚未檢查",
            "statusline_waiting_for_data" => "等待 statusline 資料",
            "statusline_setup_incomplete" => "設定未完成",
            "setup_journal_unreadable" => "設定記錄無法讀取",
            "statusline_rate_limits_missing" => "來源未回報額度",
            "statusline_configuration_conflict" => "設定衝突",
            "helper_unavailable" => "helper 遺失",
            "helper_file_conflict" => "helper 衝突",
            "helper_read_failure" => "helper 無法讀取",
            "codex_not_logged_in" => "尚未登入",
            "codex_account_unavailable" => "帳號資料不可用",
            "codex_limit_bucket_missing" => "帳號未提供額度",
            "window_not_reported" or "five_hour_window_not_reported" => "來源未回報此視窗",
            "provider_not_checked" => "尚未查詢",
            _ => "資料待確認"
        }
    };

    private static (string Reason, string Recommendation) DescribeCause(ProviderId providerId, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return ("尚未收到額度來源結果。", "重新整理或確認來源程式已安裝並可使用。");
        }

        var cause = (providerId, code) switch
        {
            (ProviderId.Claude, "claude_cli_not_found") => (
                "找不到 Claude Code CLI。",
                "確認已安裝 Claude Code 並在 PATH；Monitor 不會代為安裝。"),
            (ProviderId.Claude, "claude_settings_not_inspected") => (
                "Claude settings.json 尚未檢查；Monitor 不會在背景讀取這份可能含私人設定的檔案。",
                "按「檢查／設定 Claude 額度來源」並確認本機檢查；若要啟用 statusLine，Monitor 會先備份原檔。"),
            (ProviderId.Claude, "statusline_not_configured") => (
                "尚未設定 Claude Code 官方 statusline。",
                "按「設定 Claude 額度來源」啟用本機 helper；既有設定會先備份，遇到自訂 statusLine 時會停止。"),
            (ProviderId.Claude, "statusline_setup_incomplete") => (
                "Claude 額度設定在寫入期間中斷，復原記錄仍在本機。",
                "按「復原未完成的 Claude 設定」清理 Monitor 自己的記錄；使用者變更會保留。"),
            (ProviderId.Claude, "setup_journal_unreadable") => (
                "Monitor 的 Claude 設定記錄損毀或無法讀取；為避免覆寫，設定操作已停止。",
                "檢查本機 %LOCALAPPDATA%\\AiQuotaMonitor 設定記錄與 Claude 備份；不要直接覆寫 settings.json。"),
            (ProviderId.Claude, "statusline_waiting_for_data") => (
                "statusline 已設定，但 Claude Code 尚未傳來含額度欄位的更新。",
                "在原本使用的 Claude Code session 等待 statusline 更新；不要為取得數值額外觸發推論。"),
            (ProviderId.Claude, "timeout") => (
                "statusline 已設定，但 15 分鐘內沒有收到資料。",
                "確認 Claude Code session 正常運作且 statusLine command 可執行；在下一次正常使用後再刷新，不要為取得數值額外觸發推論。"),
            (ProviderId.Claude, "statusline_helper_failure") => (
                "Claude statusline helper 無法處理輸入。",
                "確認 Monitor 隨附 helper 未被移動或封鎖；重新開啟程式並等待下一次正常 statusline 更新。"),
            (ProviderId.Claude, "statusline_rate_limits_missing") => (
                "Claude Code 未回報 rate_limits 額度欄位。",
                "官方欄位可能尚未在首次 API 回覆後出現，或目前方案不提供這些欄位；確認 Claude 帳號方案與官方 statusline 輸入。"),
            (ProviderId.Claude, "statusline_configuration_conflict") => (
                "偵測到既有自訂 Claude statusLine；Monitor 保留原設定，未覆寫。",
                "如要接收額度，請自行將 Monitor helper 整合至原 statusline command，或先備份後調整原設定。"),
            (ProviderId.Claude, "helper_unavailable") => (
                "Claude statusline 指向的 Monitor helper 不存在或未隨程式安裝。",
                "從完整 Release portable ZIP 重新解壓並以 Claude 卡片重新設定；Monitor 不會覆寫現有檔案。"),
            (ProviderId.Claude, "helper_file_conflict") => (
                "Monitor helper 路徑的檔案內容已變更。",
                "為避免覆寫，Monitor 未替換檔案；請檢查來源或復原 statusLine 後，再手動處理衝突。"),
            (ProviderId.Claude, "helper_read_failure") => (
                "無法安全檢查 Claude statusline helper。",
                "確認本機檔案權限後重新檢查；Monitor 不會顯示或記錄檔案內容。"),
            (ProviderId.Claude, "statusline_already_configured") => (
                "Claude statusline 已存在，但目前尚未確認它是否傳送 Monitor 所需欄位。",
                "確認 command 使用 Monitor helper，且 Claude Code JSON 含 rate_limits.five_hour 與 rate_limits.seven_day。"),
            (ProviderId.Claude, "settings_parse_failure") => (
                "Claude settings.json 不是有效 JSON，Monitor 沒有修改檔案。",
                "先用 Claude Code 或文字編輯器修復該 JSON，然後重新檢查。"),
            (ProviderId.Claude, "settings_read_failure") => (
                "無法安全讀取 Claude settings.json。",
                "確認目前 Windows 使用者有該檔案的讀取權限；Monitor 未修改該檔案。"),
            (ProviderId.Claude, "settings_write_failure") => (
                "無法安全更新 Claude settings.json。",
                "檢查檔案權限與本機備份，再重試；其他設定保持原樣。"),
            (ProviderId.Claude, "window_not_reported") => (
                "Claude statusline 沒有回報這個額度視窗。",
                "確認該帳號與 Claude Code 版本有提供此欄位；Monitor 不會用其他視窗推算。"),
            (ProviderId.Claude, "unsupported_statusline_data") => (
                "Claude statusline JSON 結構不符合目前支援的官方格式。",
                "確認 Claude Code 使用官方 statusline JSON，並查看版本相符的來源說明。"),
            (ProviderId.Codex, "codex_cli_not_found") => (
                "找不到可執行的 Codex CLI。",
                "確認 Codex CLI 或 Codex Desktop 已安裝；可檢查官方安裝路徑、PATH 與 CODEX_CLI_PATH。Monitor 不會安裝 CLI。"),
            (ProviderId.Codex, "codex_cli_unavailable") => (
                "找到 Codex CLI 路徑，但 Windows 無法啟動該程式。",
                "確認 Codex 安裝完整、檔案可執行且未被移除，再重新整理。"),
            (ProviderId.Codex, "codex_not_logged_in") => (
                "Codex 回報目前沒有已登入的帳號。",
                "在 Codex 官方 CLI 或 Desktop 正常登入後重新整理；Monitor 不會啟動登入或讀取憑證檔。"),
            (ProviderId.Codex, "rpc_method_not_supported") => (
                "目前 Codex App Server 不支援 account/rateLimits/read。",
                "更新官方 Codex CLI 或 Desktop 到提供此方法的版本，再重試。"),
            (ProviderId.Codex, "timeout") => (
                "Codex App Server 查詢逾時。",
                "確認 Codex CLI 可啟動與網路可用，稍後重新整理。"),
            (ProviderId.Codex, "app_server_unreachable") => (
                "Codex App Server 未能正常回應本機唯讀查詢。",
                "重新開啟 Codex CLI 或 Desktop，確認安裝路徑後再次刷新。"),
            (ProviderId.Codex, "network_unreachable") => (
                "Codex App Server 無法連線取得帳號額度資料。",
                "確認目前網路與 Codex 服務可用，稍後重新整理。"),
            (ProviderId.Codex, "codex_account_unavailable") => (
                "Codex 無法提供登入狀態或額度資料。",
                "在 Codex 官方程式確認登入狀態與網路，再重新整理；Monitor 不會讀取 Token 或自動登入。"),
            (ProviderId.Codex, "codex_limit_bucket_missing") => (
                "Codex 已回應，但沒有回報 Codex 額度 bucket。",
                "目前帳號方案或 Codex 版本可能不提供額度；Monitor 不會從其他 bucket 推估。"),
            (ProviderId.Codex, "window_duration_unavailable") => (
                "Codex 回傳的視窗長度不在支援範圍。",
                "請更新 Codex 或 Monitor；不會根據 primary／secondary 順序猜測週期。"),
            (ProviderId.Codex, "five_hour_window_not_reported") => (
                "Codex 未回報 5 小時視窗。",
                "確認目前帳號或 Codex 版本支援該視窗；不會用每週額度推算。"),
            (ProviderId.Codex, "window_not_reported") => (
                "Codex 未回報此額度視窗。",
                "確認目前帳號方案與 Codex 版本是否提供此視窗。"),
            (_, "parse_failure") => (
                "額度資料解析失敗，回應未被當成有效額度。",
                "更新來源程式後重新整理；診斷不會保存或顯示原始回應。"),
            (_, "invalid_used_percent") => (
                "來源提供的已用百分比格式無效。",
                "等待來源提供合法的 0～100 數值；Monitor 不會修正或推測百分比。"),
            (_, "invalid_reset_timestamp") => (
                "來源提供的重置時間格式無效；百分比可用，但倒數不顯示。",
                "等待來源提供官方格式的 Unix 秒時間戳。"),
            (_, "duplicate_window") => (
                "來源重複回報相同額度週期，該筆資料已拒絕。",
                "更新來源版本並重新整理。"),
            (_, "unsupported_response") => (
                "來源回應格式不受支援。",
                "更新來源程式並重新整理；不會將未知欄位解讀成額度。"),
            (_, "stale_data") => (
                "上次成功的來源資料已超過 15 分鐘。",
                "等待 Claude Code 下次 statusline 更新，或重新整理 Codex 資料。"),
            (_, "provider_not_checked") => (
                "來源尚未完成首次查詢。",
                "等待首次唯讀刷新完成。"),
            (_, "provider_failure") => (
                "來源查詢發生未分類錯誤。",
                "重新整理；如持續發生，提供版本與這個安全錯誤代碼供診斷，不要附原始回應。"),
            _ => (
                "來源回報未分類狀態。",
                "確認來源已安裝並可使用，稍後重新整理；不會顯示或記錄原始額度回應。")
        };

        return ($"{cause.Item1}（{code}）", cause.Item2);
    }
}
