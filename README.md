# Windows AI Quota Monitor

Windows 11 x64 桌面程式，以繁體中文顯示 Claude Code 與 OpenAI Codex 可取得的 5 小時及每週額度。只顯示經來源資料證實的百分比與重置時間；缺少或不支援的欄位維持 `UNKNOWN`／`N/A`，不以假資料、本機 token 計數或歷史平均冒充訂閱額度。

> v0.1.0 保留為原始 prerelease。v0.1.1 候選修正來源 SHA `8aef7db2a25f637c8df8231b1f29fd540e9881f5` 已在 GitHub Windows Server 2025 runner 完成 packaged EXE UI smoke、故障反向測試與 SHA-256 同一性驗證（[Actions run 39553296900](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39553296900)）。EXE SHA-256 為 `fd03e4d6ee9357e67bc3248a98840b39eee058a2911efd0bd55e538d1021282b`，portable ZIP 為 `c72e1a80fdc4ee0bd2100654eb75b316fb5ef48705548a37471c2918f1540ba9`；尚未發布，仍須完成精確 PR head review、Squash Merge 與 merge 後 tag Release Gate。Windows runner 通過不代表 Windows 11 實機 UAT；請依 [UAT 清單](docs/UAT_WINDOWS11.md)確認。

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

Monitor 直接啟動本機已安裝的 `codex.exe app-server`，使用官方 `initialize`／`initialized` 握手及 `account/rateLimits/read` 唯讀方法。映射依來源 `windowDurationMins`：300 分鐘為 5 小時、10080 分鐘為每週；只有有效 `usedPercent` 才計算 remaining。Codex app-server 是實驗性協定；找不到 CLI、帳號不可用、未知 schema 或不支援方法時顯示 UNKNOWN、N/A 或 UNSUPPORTED。

程式不讀 `auth.json`，不取出 Token、不送登入／thread／turn／model／inference RPC。app-server 可能依 Codex 本身的登入狀態連線讀取額度；Monitor 不另行選擇或猜測網路 API。使用者須自行安裝並設定 Codex；Monitor 不會安裝、修復或登入 Codex。

### Claude Code（可選手動設定）

Claude Code 官方 [statusline JSON](https://code.claude.com/docs/en/statusline) 在資料可得時包含 5 小時和 7 日 rate limit 欄位。Monitor 附有 `claude-statusline.ps1`，只接收使用者選擇的官方 statusline 輸入並把兩個額度視窗送到限本機目前使用者的 pipe。未設定 statusline、欄位缺席或資料逾 15 分鐘時顯示 UNKNOWN、N/A 或資料已過期；Monitor 不會啟動 Claude Code 來要求新資料。

要啟用，先解壓 portable ZIP，然後依官方格式在 Claude Code 使用者設定 `~/.claude/settings.json` 手動加入 `statusLine` 區塊，並將範例路徑換成實際 helper 路徑：

```json
{
  "statusLine": {
    "type": "command",
    "command": "powershell -NoProfile -File C:/Tools/AiQuotaMonitor/claude-statusline.ps1"
  }
}
```

Monitor 不會讀取或改寫 Claude 設定。若已有自訂 statusline command，單獨指定 helper 會取代原顯示，請先自行整合原指令後再設定。

Claude 首次 API 回覆前，statusline 的 rate limit 欄位可能缺席。此整合不呼叫未公開的 OAuth usage endpoint，不讀取 Claude credential files，不登入／refresh Token，也不執行推論。

## 下載與執行

1. 從 [GitHub Releases](https://github.com/henryfir456/ai-quota-monitor/releases) 下載 `win-x64-portable.zip`，另可下載獨立 `AiQuotaMonitor.exe`。
2. 核對同一 Release 的 `SHA256SUMS.txt`，再將 ZIP 解壓至使用者可寫入的資料夾。
3. 雙擊 `AiQuotaMonitor.exe`。設定保存在 `%LOCALAPPDATA%\AiQuotaMonitor\settings.json`；portable 資料夾不需要安裝。
4. 若要離開常駐程式，從系統匣選「結束程式」。視窗右上角關閉按鈕會縮至系統匣。

Release 附版本、來源 commit、依賴清單、SBOM、Windows 啟動測試報告、最終 SHA-256 和 unsigned notice。GitHub Actions 先完成 build → package → 啟動 smoke/故障反向測試 → SHA-256 驗證，才建立 prerelease；驗證後沿用完全相同的 EXE 與 ZIP。v0.1.0 未程式碼簽章；SHA-256 只能驗證檔案完整性，不驗證發行者身分。

## 安全與隱私

- 不讀取、記錄或複製 OAuth Token，不讀取 Codex／Claude 憑證檔。
- 不自動登入、refresh Token、修復、安裝／更新 CLI、執行付費推論或傳送遙測。
- Claude helper 只傳百分比與 reset epoch seconds；session ID、prompt ID、transcript path 和工作目錄不傳入 Monitor。
- Codex 子程序只取得必要的 OS 路徑與使用者 profile 環境變數；stderr 與原始 RPC 錯誤不保存。
- 本機只保存主題、刷新、浮動視窗和提醒偏好等非敏感設定。

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
