# AI Quota Monitor v0.1.0 prerelease

Windows 11 x64 可攜版，使用 .NET 8 WPF。內含 Claude Code 與 Codex 額度卡、系統匣、精簡浮動面板、深／淺主題、使用者可設定刷新與低額度提醒。

## 來源限制

- Codex 以官方 `codex app-server` `account/rateLimits/read` 讀取帳號 rate-limit 資料。App-server 是實驗性協定；CLI 不存在、資料不可得或 schema 不符時顯示 UNKNOWN／UNSUPPORTED。
- Claude 使用官方 statusline JSON 的可選使用者設定 bridge。預設未修改 Claude 設定，未配置時顯示 UNKNOWN/N/A。
- 不讀取 OAuth credential files、不登入、不修復／安裝 CLI、不執行推論、不使用未公開 OAuth usage API。

## 驗證與限制

Release 附件包含 Windows runner TRX／測試摘要、source commit、SBOM、依賴清單、SHA-256 和未簽章告知。Windows CI 通過不等同 Windows 11 使用者實機 UAT；實機 UAT 狀態維持 `HUMAN_TEST_PENDING`，完整項目見 `UAT_WINDOWS11.md`。

此 prerelease 尚未程式碼簽章。SHA-256 可檢查下載完整性，不能證明發行者身分。
