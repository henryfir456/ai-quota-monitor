# 更新紀錄

遵循 Keep a Changelog 格式。Release tag、app version 與此文件版本保持一致。

## [Unreleased]

### 新增
- 建立 .NET 8 WPF/MVVM、Core DTO、provider adapters、合成測試與 Windows CI。
- Codex 官方 app-server 唯讀查詢與欄位證據文件。
- Claude 官方 statusline JSON 可選本機 bridge；預設不修改 Claude 設定。
- 繁體中文雙卡、來源狀態、刷新／收合／主題／浮動面板、系統匣、設定、低額度提醒與單一執行個體。
- 加入安全／隱私／威脅模型、NuGet audit、Gitleaks、portable Release workflow 與 UAT checklist。

### 安全
- 沒有來源資料時使用 UNKNOWN／N/A；無效百分比或 reset 不推測。
- 不讀取 Token 檔、不記錄 raw provider response／stderr，不登入、安裝 CLI、修復或執行 inference。
- Codex app-server process 採環境變數 allowlist；Claude helper 只傳 rate limit 欄位。

## [0.1.0] - 2026-10-21

Windows x64 prerelease 候選。包含 Codex 官方 app-server readonly provider、Claude Code 官方 statusline bridge（使用者選擇性配置）、繁體中文 WPF 卡片、系統匣、浮動面板、深／淺主題、使用者提醒設定及 self-contained portable 發行流程。Windows 11 實機 UAT 狀態仍為 `HUMAN_TEST_PENDING`。
