# DECISIONS｜重大設計決策

## ADR-000｜採用最小可用 AI Agent 治理
- 日期：2026-10-11
- 決策：以精簡 `AGENTS.md` 定義安全底線，另以 facts、state、decisions、review、deployment 承載各自職責。
- 原因：避免龐大且不易更新的單一 Prompt。

## ADR-001｜GitHub 保護規則狀態採時點確認並逐輪重查
- 日期：2026-10-19
- 決策：Repo 保護及 Required Checks 必須按當前 GitHub 狀態與有效政策證據判定；未知不得推定不存在或通過。

## ADR-002｜AI Quota Monitor 技術架構
- 日期：2026-10-21
- 情境：新 Repo 起始於通用模板，目標為可攜 Windows 11 桌面額度監控器。
- 決策：使用 .NET 8 WPF/MVVM；分為 `App`、不依賴 UI 的 `Core` DTO、`Providers` 與 `tests`。不使用 Electron、後端、遙測或管理員權限。
- 原因：依 GitHub Issue #1 的已確認架構。
- 影響：Windows Desktop SDK、WPF build 與實機 UAT 必須由 Windows 環境驗證。

## ADR-003｜額度 DTO fail closed
- 日期：2026-10-21
- 決策：`RemainingPercent` nullable 且限 0～100；沒有 provider 來源欄位時回傳 null、`UNKNOWN`/`UNSUPPORTED`/`ERROR`。僅在實際來源回報有效 used 值時轉為 remaining。
- 原因：不以 token 使用量、假資料或缺欄位推算訂閱額度。
- 影響：UI 對缺失、過期、失敗及不支援欄位必須清楚標示。

## ADR-004｜憑證與授權邊界
- 日期：2026-10-21
- 決策：不讀取 Token 檔、不執行登入／修復／CLI 安裝、不執行推論；採 Issue #2、#3 明確核實的唯讀來源。若需 opt-in OAuth，必須另立 Issue/PR 並取得使用者核准。
- 影響：沒有安全且已核實來源的 provider/window 只能顯示 UNKNOWN 或 N/A。

## ADR-005｜Codex 官方 app-server quota query
- 日期：2026-10-21
- 決策：依官方 initialize capability 協定宣告 `experimentalApi=true`，先以 `account/read`（`refreshToken=false`）確認登入，再呼叫官方 `account/rateLimits/read`；只依 `windowDurationMins` 映射週期；每次使用短生命週期子程序、環境 allowlist、stderr 丟棄與 sanitized error codes。
- 原因：不猜測 primary/secondary 的固定含義，不讀取 `auth.json`，不使用參考專案的非官方 fallback。
- 風險：app-server 標示為 experimental；目標 Windows CLI 版本未在本開發環境驗證。

## ADR-006｜Claude 官方 statusline opt-in bridge
- 日期：2026-10-21
- 決策：提供 PowerShell helper 接收官方 statusline stdin JSON。預設唯讀檢查設定；使用者確認後，完整備份原 `settings.json`、保留其他欄位並加入 helper；遇既有自訂 `statusLine` 或內容變更即停止，並提供只移除未變更 Monitor entry 的復原。
- 原因：官方 statusline 是無 Token 的本機資料來源；未配置或欄位未出現時保留 UNKNOWN/N/A。
- 影響：若使用者已有 statusline command，Monitor 保留原設定且不自動整合；Monitor 不啟動 Claude CLI 或推論。

## ADR-007｜Prerelease 與人工 UAT 的分界
- 日期：2026-10-21
- 決策：每個 Windows prerelease 都由 Release workflow restore/build/test/publish/package，並在最終 packaged EXE 的 smoke 與 SHA-256 Gate 通過後建立；Windows 11 實機 UAT 清單維持 `HUMAN_TEST_PENDING`，不宣稱為已驗收。
- 原因：使用者要求可攜 Windows 產物和 prerelease，同時明確禁止以 CI 或 Linux 冒充 Windows 實測。
- 影響：prerelease 文件揭露未簽章與 UAT pending；stable release 不在本次授權範圍。

## ADR-008｜來源失敗可診斷且不保留舊額度
- 日期：2026-10-21
- 決策：每個 provider/window 以固定錯誤碼表示未安裝、未設定、未登入、逾時、解析失敗、過期及不支援；UI 對應繁中原因與建議。只有 fresh、有效百分比更新成功時間；失敗時不把舊百分比當成最新值。
- 原因：使用者回報兩個來源都只顯示 UNKNOWN/N/A，無法確認原因。
- 影響：自動 fixture 可驗證協定與狀態分類，但不代表任何實際 Claude/Codex 帳號已成功提供額度。
