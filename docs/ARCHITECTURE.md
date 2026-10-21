# ARCHITECTURE｜系統架構

## 專案概觀

AI Quota Monitor 是 Windows 11 x64、.NET 8 WPF/MVVM 桌面程式。UI 只呈現經來源驗證的 Claude Code 與 Codex 5 小時／每週 quota 欄位；資料缺席、過期、失敗、不支援及合成 demo 都有獨立狀態。應用不提供登入、CLI 安裝／修復、推論、遠端後端或遙測。

## 元件

- `AiQuotaMonitor.App`：WPF 主視窗、系統匣、單一執行個體、精簡置頂面板、主題、提醒與 `%LOCALAPPDATA%` 非敏感設定；不直接解析供應商原始回覆。
- `AiQuotaMonitor.Core`：不依賴 WPF 的 provider/window/status DTO 與 nullable、有限範圍 quota 百分比契約。
- `AiQuotaMonitor.Providers`：每來源 10 秒 timeout 的隔離 coordinator、Codex app-server JSONL client/parser、Claude statusline parser/provider/named pipe。
- `AiQuotaMonitor.Tests`：合成 Codex／Claude fixture、RPC、process environment、timeout、設定和 Windows PowerShell bridge 測試。
- `tests/scripts`：Windows PowerShell helper parser/合成輸入驗證、Release SBOM 與 package 建置工具。

## 資料流

```text
Claude Code statusline stdin JSON ──使用者設定──▶ PowerShell allowlist helper
                                                       │ rate limits only
                                                       ▼ current-user named pipe
Codex CLI ── official app-server JSONL ──▶ provider adapters
                                               │
                                               ▼
WPF ViewModel ◀── QuotaRefreshService ◀── ProviderSnapshot / QuotaObservation
```

`RemainingPercent` 可為 null，只接受 0～100 的有限數字。來源若只回報 used 百分比，adapter 只在值有效時換算 `100 - used`；缺值不補估。`ResetAtUtc` 可為 null，Unix seconds 只依各來源官方 schema 解析；不以毫秒外觀猜格式。錯誤碼為固定內部代碼，不回傳 raw server message。

## Provider 邊界

### Codex

每次查詢以絕對 `codex.exe` 路徑啟動短生命週期 `app-server` 子程序，送含 `capabilities.experimentalApi=true` 的 `initialize`、`initialized`、`account/read`（`refreshToken=false`）與 `account/rateLimits/read`，讀一個 quota response 後終止整個 process tree。`account/read` 只用於辨識帳號是否為 null，不保留帳號欄位。依官方 `windowDurationMins` 映射 300／10080 分鐘。stdout 僅供 RPC parser；stderr 丟棄；子程序環境只傳 OS 路徑及使用者 profile。Monitor 不開啟 Codex credentials 檔，也不取出／保存 Token。

### Claude Code

官方 statusline 資料由 Claude Code 在本機以 JSON stdin 呼叫使用者設定的 command。Monitor 啟動時不讀 `~/.claude/settings.json`；使用者在 Claude 卡片確認本機檢查後才辨識來源狀態。啟用時先建立 recovery journal 和完整檔案備份，再保留其他 JSON 欄位並以原子寫入加入隨附 helper；新建 settings 使用 no-overwrite move，既有的自訂 `statusLine` 會阻止寫入。helper 只傳合法的 `five_hour`、`seven_day` rate limits 欄位或固定安全錯誤碼，不轉送任意原始值；設定後 15 分鐘未收到資料會逾時，最後成功資料超過 15 分鐘則標為過期。

## 刷新與狀態

- 啟動後立刻查詢，預設 300 秒，使用者可設 60～1800 秒；手動刷新需間隔至少 10 秒。
- provider 並行且各自 10 秒 timeout；防重疊；錯誤退避上限 30 分鐘。睡眠恢復和網路恢復可提早查詢。
- 刷新失敗或來源不支援不會回寫假百分比。只有最新、實際、非 demo 來源可以觸發使用者 opt-in 低額度提醒。
- `UNKNOWN`、`UNSUPPORTED/N/A`、`ERROR`、`STALE` 與 `Fresh` 分別保留；過期或錯誤資料不提醒。

## 隱私、設定與發行

程式不自建網路 endpoint、資料庫、遙測或更新服務。Codex app-server 依它自己的產品實作管理認證與網路呼叫；Claude statusline 是本機資料。Monitor 偏好設定不存 Token、帳號識別或原始 provider 回覆；Claude 設定檔備份只有使用者確認啟用 statusline 後才會建立，完整留在本機。具體資料處理見 [PRIVACY.md](../PRIVACY.md) 和 [THREAT_MODEL.md](THREAT_MODEL.md)。

Windows CI 執行 restore/build/test、PowerShell 5.1 parser 與 synthetic bridge、NuGet vulnerability audit；Ubuntu job 執行 repository governance tests 和 full-history Gitleaks secret scan。tag workflow 產生 self-contained win-x64 EXE、portable ZIP、SBOM、依賴清單、SHA-256 和測試摘要。CI／Release 不等同 Windows 11 個人裝置 UAT。
