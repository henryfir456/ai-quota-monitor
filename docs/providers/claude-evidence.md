# Claude 額度來源證據

觀測日期：2026-10-21

## 已核實來源

- Anthropic 官方 [Claude Code setup 文件](https://code.claude.com/docs/en/setup) 的 Windows 卸載命令列出原生安裝 launcher `%USERPROFILE%\.local\bin\claude.exe`，版本檔放 `%USERPROFILE%\.local\share\claude`。Native Windows 安裝不需系統管理員；Monitor 也會檢查常見 `%APPDATA%\npm\claude.cmd`、`%LOCALAPPDATA%\Programs\Claude\claude.exe` 與 PATH CLI，僅判斷檔案存在，不執行 CLI。使用者若在 WSL 安裝，需在 Windows 端另有 Claude Code CLI 才能觸發 Windows statusline helper。
- Anthropic 官方 [Claude Code status line 文件](https://code.claude.com/docs/en/statusline) 說明：Claude Code 將本機 session JSON 經標準輸入傳給使用者配置的 statusline command，並顯示 command 的標準輸出。文件的 `rate_limits` 欄位表列 `five_hour.used_percentage`、`seven_day.used_percentage`（0～100）及各自的 `resets_at`（Unix epoch 秒）。欄位可能在首次 API 回覆前為空；狀態列由事件觸發，也可由使用者設定 `refreshInterval`。
- 官方文件要求在 `~/.claude/settings.json` 或 project settings 設定 `statusLine`。Monitor 啟動時讀取使用者設定檔，只檢查 statusLine/衝突並在 UI 顯示狀態；使用者確認啟用後才建立完整本機備份並加入隨附 helper。既有自訂 statusLine、無效 JSON 或 helper 衝突時 fail closed。復原只移除與 Monitor 記錄完全相符的設定，不自動刪除備份或 helper。
- Claude bridge 只把兩個已核實視窗的百分比與 reset epoch seconds 寫入限本機目前使用者連線的 named pipe；不傳送 session ID、prompt ID、transcript path、工作目錄、Token 或完整 statusline JSON。程式未啟動時，statusline command 仍可輸出其本機百分比摘要，但不會排隊或寫入檔案。
- [Trafalgardi/ai-usage-widget v2.3.0](https://github.com/Trafalgardi/ai-usage-widget/tree/v2.3.0) 的 Claude adapter 讀取 Claude credentials 檔並呼叫未公開的 OAuth usage endpoint。這些存取方式不屬於本產品的允許資料來源，未複製、未執行其 EXE，也未呼叫該 endpoint。

## 版本與支援狀態

- 官方文件所列欄位與 Windows native launcher 位置是本 adapter 的依據；使用者安裝的 Claude Code 實際版本與其帳號是否回報這些欄位：`UNKNOWN`，本次沒有執行 `claude --version` 或啟動 Claude session。
- Claude statusline 只在 Claude Code 有事件／使用者設定的刷新間隔時執行；Monitor 不得藉由啟動 CLI、登入或推論來要求新資料。未設定、已設定但 15 分鐘未收資料、欄位缺席、解析失敗及最後成功資料逾 15 分鐘會以各自的狀態碼和建議顯示。
- 使用者若已有自訂 `statusLine` command，Monitor 會保留原設定並停止設定，不嘗試覆寫或自動整合。使用者需自行調整衝突或將 helper 整合到原命令。

## 欄位映射與合成回覆

只接受官方 JSON 欄位 `rate_limits.five_hour` 與 `rate_limits.seven_day`。`used_percentage` 必須為有限的 0～100 數字，才轉為 `RemainingPercent = 100 - used_percentage`；`resets_at` 只接受文件所定義的 Unix 秒，缺席顯示 N/A，毫秒形狀或其他格式不推測。七日視窗映射至 UI 每週額度。合成測試 fixture `tests/AiQuotaMonitor.Tests/Fixtures/claude-statusline-usage.json` 不含真實 session 或帳號資料。

## 不支援範圍

- 沒有啟用 Claude Code session 或官方 statusline 欄位不可得時，額度保持 UNKNOWN/N/A。
- 本版本不讀取 `~/.claude/.credentials.json`、`~/.config/claude/.credentials.json`、其他憑證檔或環境中的 OAuth Token；不呼叫非公開 OAuth usage endpoint、不登入、不 refresh Token、不修復／安裝 Claude CLI、不自動發起付費推論。使用者明確確認前不修改設定；確認後只更新 `statusLine` 並保留原檔備份。
- 參考專案只用於識別被明確排除的憑證檔／非公開 API 路徑；不執行第三方 EXE。

## 應用限制

此來源是 Claude Code 官方支援的本機 statusline JSON，不是桌面帳號狀態 API。只有 Claude Code 已提供欄位時，Monitor 才可顯示該次觀測；初次查詢、未配置或長時間無 session 更新不代表 quota 為零或 100%。合成 parser/pipe 測試不證明任何真實 Claude 帳號已回報欄位；使用者 Windows 11 帳號驗證仍待 UAT。
