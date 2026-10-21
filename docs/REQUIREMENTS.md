# REQUIREMENTS｜需求與邊界

## 問題與目標

提供 Windows 11 x64 可解壓、雙擊執行的桌面程式，以繁體中文查看 Claude Code 與 Codex 真實 5 小時及 weekly quota，顯示來源、剩餘百分比、reset 倒數與資料健康狀態。

## 驗收條件

- .NET 8 WPF/MVVM；App、純 DTO Core、Providers、tests 分層；Windows x64 非管理員啟動。
- 啟動立即 refresh，預設 300 秒；UI 可設定 60～1800 秒；manual refresh 10 秒 throttle；每個來源 10 秒 timeout、獨立錯誤隔離、防重疊和 bounded retry/backoff ≤30 分鐘。
- Claude/Codex 雙卡；5 小時／每週百分比與進度 bar、Asia/Taipei reset 時間、倒數、last updated、source；used/remaining toggle。
- `UNKNOWN`、`N/A/UNSUPPORTED`、`STALE`、`ERROR`、`Fresh` 不混淆；低於 20% 橘色、低於 10% 紅色並有文字；只對 Fresh 實際資料發使用者 opt-in、去重的提醒。
- 系統匣常駐、關閉隱藏、tray exit 清理；單一執行個體，二次啟動聚焦原視窗；精簡置頂可拖曳浮動面板、位置恢復、DPI／keyboard accessibility。
- 深色／淺色主題；使用者非敏感設定採 schema-version、atomic write、`%LOCALAPPDATA%`；低提醒及 Windows startup 預設 off，只有使用者手動選擇後才設 HKCU Run。
- Codex 額度只能依官方 app-server `account/rateLimits/read` response/schema 映射；不讀 `auth.json`，不顯示未核實 windows。
- Claude 額度只能依官方 statusline JSON 欄位；helper 由使用者自行配置且只傳 allowlist 欄位；不使用未公開 OAuth endpoint。
- Windows Release workflow restore/build/test/audit/PowerShell test 通過；發布 self-contained portable ZIP、EXE、SHA-256、SPDX SBOM、依賴清單、測試報告與 prerelease。
- Windows 11 實機驗收逐項另列；CI/Linux 不能代替，未執行的項目維持 `HUMAN_TEST_PENDING`。

## 安全邊界與 Out of Scope

- 不讀取、記錄或複製 OAuth Token／credential files，不自動登入或 refresh。
- 不安裝／更新／修復 Claude/Codex CLI；不執行付費 inference；不自建 endpoint、後端、遙測或自動更新。
- 不用本機 token counter、推論歷史平均或 mock 數值假裝供應商 quota。
- 不為取得資料自動覆寫 Claude statusline/user settings。
- 不宣稱程式碼簽章、Defender/SmartScreen 通過或 Windows 11 真機 UAT 通過。
- stable release、安裝器、正式資料變更、Secrets 與資料 migration 不在本次授權範圍。

## Issue 執行順序

1. #1 foundation、DTO、Windows CI。
2. #2 Codex 官方來源研究／安全唯讀 adapter。
3. #3 Claude 官方來源研究／可選本機 statusline adapter。
4. #6 威脅模型、PRIVACY/SECURITY、敏感資訊與依賴檢查。
5. #4 WPF UI。
6. #5 系統匣、設定和常駐刷新。
7. #7 Windows portable publish、prerelease、checksum、報告及 UAT 清單。
