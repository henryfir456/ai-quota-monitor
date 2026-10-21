# Windows AI Quota Monitor

Windows 11 x64 桌面程式，以繁體中文顯示 Claude Code 與 OpenAI Codex 可取得的 5 小時及每週額度。只顯示經來源資料證實的百分比與重置時間；缺少或不支援的欄位維持 `UNKNOWN`／`N/A`，不以假資料、本機 token 計數或歷史平均冒充訂閱額度。

> v0.1.0 原 prerelease 會保留不變。請在 [GitHub Releases](https://github.com/henryfir456/ai-quota-monitor/releases) 取得目前版本；每個 Release 都附 Windows 測試報告與 SHA-256。Windows Runner 通過不等於 Windows 11 實機或真實供應商帳號驗證；請依 [UAT 清單](docs/UAT_WINDOWS11.md)確認。

## 功能

- Claude Code 與 Codex 雙卡，顯示 5 小時／每週剩餘百分比、進度條、重置時間、倒數、來源與觀測時間。
- 可切換已用／剩餘百分比、深色／淺色主題、收合卡片及可選置頂的精簡浮動面板。
- 系統匣常駐、單一執行個體、手動刷新 10 秒節流、預設 300 秒輪詢（設定範圍 60～1800 秒）。
- 每個來源獨立 10 秒逾時；失敗時有上限 30 分鐘的退避；恢復網路或離開睡眠時會提前刷新。
- 低額度提醒需使用者開啟；每個供應商／視窗／門檻／reset window 去重。過期、UNKNOWN、ERROR、N/A 或示意資料不會觸發提醒。
- 關閉主視窗會縮至系統匣；系統匣可顯示／隱藏主視窗與精簡面板、暫停／恢復自動監控、刷新、設定或結束程式。暫停時停止自動刷新與提醒，仍可手動刷新；恢復時立即刷新。暫停只在本次執行期間有效，重新啟動後恢復自動監控。
- 不要求管理員權限；登入 Windows 時啟動預設關閉。

## 真實資料來源

### Codex

Monitor 從 PATH、`CODEX_CLI_PATH`、常見 CLI 安裝位置、`CODEX_HOME`（或預設 `%USERPROFILE%\.codex`）下的 standalone/app-server 套件，或可列舉的 Codex Desktop Windows 套件位置尋找 `codex.exe`，再以 stdio 啟動 `codex.exe app-server`。WindowsApps 套件可能受 Windows ACL 限制；若系統拒絕啟動，卡片會顯示程式無法啟動，不會複製或修改安裝內容。初始化會宣告 `experimentalApi`，先用 `account/read` 的 `refreshToken=false` 確認登入狀態，再呼叫唯讀 `account/rateLimits/read`。映射依來源 `windowDurationMins`：300 分鐘為 5 小時、10080 分鐘為每週；只有有效 `usedPercent` 才計算剩餘額度。Codex app-server 是實驗性協定；找不到 CLI、未登入、帳號不可用、逾時、未知 schema 或不支援方法時，卡片會顯示狀態、來源、最近成功更新時間、安全錯誤代碼與繁中處理建議。

程式不讀 `auth.json`，不取出 Token、不送登入／thread／turn／model／inference RPC。app-server 可能依 Codex 本身的登入狀態連線讀取額度；Monitor 不另行選擇或猜測網路 API。使用者須自行安裝並設定 Codex；Monitor 不會安裝、修復或登入 Codex。

### Claude Code（使用者選擇啟用）

Claude Code 官方 [statusline JSON](https://code.claude.com/docs/en/statusline) 在資料可得時包含 5 小時和 7 日 rate limit 欄位。Monitor 附有 `claude-statusline.ps1`，只接收使用者選擇的官方 statusline 輸入並把兩個額度視窗送到限本機目前使用者的 pipe。未設定 statusline、欄位缺席或資料逾 15 分鐘時顯示 UNKNOWN、N/A 或資料已過期；Monitor 不會啟動 Claude Code 來要求新資料。

Monitor 啟動時不讀取 Claude 設定。若要啟用來源，在主視窗的 Claude 卡片按「檢查／設定 Claude 額度來源」並確認本機檢查；若沒有設定衝突並繼續，Monitor 會先備份完整 `~/.claude/settings.json`，再安裝隨附的本機 helper 並加入官方 `statusLine` command。檔案可能含私人設定，備份留在本機。若偵測到既有自訂 `statusLine`、設定格式錯誤或 helper 路徑已有其他內容，Monitor 會停止且不覆寫。相同按鈕可復原 Monitor 建立且未被修改的設定；復原會保留使用者後續加入的其他欄位，完整備份會保留在本機。

Claude 首次 API 回覆前，statusline 的 rate limit 欄位可能缺席，部分方案也可能不提供額度欄位；Monitor 不會以推測補值。卡片會區分未安裝、未設定、helper 遺失／被修改、等待更新逾時、欄位缺席、JSON 解析失敗與設定衝突。此整合不呼叫未公開的 OAuth usage endpoint，不讀取 Claude credential files，不登入／refresh Token，也不執行推論。

## 下載與執行

1. 從 [GitHub Releases](https://github.com/henryfir456/ai-quota-monitor/releases) 下載 `win-x64-portable.zip`，另可下載獨立 `AiQuotaMonitor.exe`。
2. 核對同一 Release 的 `SHA256SUMS.txt`，再將 ZIP 解壓至使用者可寫入的資料夾。
3. 雙擊 `AiQuotaMonitor.exe`。設定保存在 `%LOCALAPPDATA%\AiQuotaMonitor\settings.json`；portable 資料夾不需要安裝。
4. 若要離開常駐程式，從系統匣選「結束程式」。視窗右上角關閉按鈕會縮至系統匣。

Release 附版本、來源 commit、依賴清單、SBOM、Windows 啟動測試報告、最終 SHA-256 和 unsigned notice。GitHub Actions 先完成 build → package → 啟動 smoke/故障反向測試 → SHA-256 驗證，才建立 prerelease；驗證後沿用完全相同的 EXE 與 ZIP。v0.1.0 未程式碼簽章；SHA-256 只能驗證檔案完整性，不驗證發行者身分。

## 安全與隱私

- 不讀取、記錄或複製 OAuth Token，不開啟 Codex／Claude 憑證檔。Claude 設定只有在使用者確認本機檢查後才讀取；確認啟用時會保留其他欄位、先將完整原檔備份在同一設定資料夾，再寫入 statusLine。
- 不自動登入、refresh Token、修復、安裝／更新 CLI、執行付費推論或傳送遙測。
- Claude helper 只傳百分比與 reset epoch seconds；session ID、prompt ID、transcript path 和工作目錄不傳入 Monitor。
- Codex 子程序只取得必要的 OS 路徑與使用者 profile 環境變數；stderr 與原始 RPC 錯誤不保存。
- 主題、刷新、浮動視窗和提醒偏好保存在 Monitor 設定檔；Claude 備份只在使用者選擇啟用 statusline 後建立，內容依原檔保留且不傳出本機。

詳細說明見 [PRIVACY.md](PRIVACY.md)、[SECURITY.md](SECURITY.md) 與 [威脅模型](docs/THREAT_MODEL.md)。

## 建置與測試

需求：Windows x64、.NET SDK 8.0.408 或相容 8.0 patch SDK、Windows Desktop build tools。

```powershell
dotnet restore AiQuotaMonitor.sln
dotnet build AiQuotaMonitor.sln -c Release --no-restore
dotnet test AiQuotaMonitor.sln -c Release --no-build
```

Windows CI 執行 XAML 唯讀 Binding 回歸測試、PowerShell parser／合成輸入驗證、NuGet vulnerability audit，並在最終 portable ZIP 建立後實際啟動其中同一 SHA-256 的 EXE。Smoke mode 使用隔離的空白偏好設定、不啟動 Provider、不讀取帳號／Token；Gate 驗證視窗標題與 handle、Claude/Codex 卡片、四個 OneWay 進度條、程序穩定存活及 Windows Application crash events，並以故意注入 `XamlParseException` 的負向案例證明 Gate 會拒絕失敗啟動。完整歷史 Gitleaks 與 repository governance tests 在 Ubuntu job 執行。CI artifact 及實際測試命令見 [測試計畫](docs/TEST_PLAN.md) 與每次 Release 附帶的 `TEST_REPORT.txt`；測試 fixture 全為合成資料。Linux build、CI PASS、靜態檢查和 source review 均不能替代 Windows 11 雙擊、關閉、系統匣、DPI、CLI 版本與真實來源 UAT。

## 專案文件

- [架構](docs/ARCHITECTURE.md) · [決策](docs/DECISIONS.md) · [需求](docs/REQUIREMENTS.md)
- [Codex 來源證據](docs/providers/codex-evidence.md) · [Claude 來源證據](docs/providers/claude-evidence.md)
- [測試報告](docs/TEST_REPORT.md) · [Windows 11 UAT](docs/UAT_WINDOWS11.md) · [安全驗證報告](docs/SECURITY_TEST_REPORT.md)
- [目前狀態](docs/PROJECT_STATE.md) · [REPO_FACTS](docs/REPO_FACTS.md) · [更新紀錄](CHANGELOG.md)
