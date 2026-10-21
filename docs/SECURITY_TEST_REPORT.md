# 安全驗證報告

報告日期：2026-10-21。下列自動驗證證據適用於完整來源 SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f`，PR run [39540874352](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39540874352)。Windows job 執行於 GitHub Windows Server 2025；Windows 11 實機結果未推定。

| Gate | 命令／方法 | 狀態 | 證據 |
| --- | --- | --- | --- |
| Release publish token isolation | Build/package and smoke jobs use `contents:read`; isolated artifact-only publisher gets `contents:write`; checkout credential persistence disabled; only v0.1.1 tag already contained in main may proceed | Workflow policy implemented; new Release workflow NOT RUN | `.github/workflows/release.yml`; verify through release run |
| Windows final-package startup gate | Offline smoke mode launches exact packaged EXE on `windows-latest`; checks UI ready signal, main window, stability, exception/events, negative startup failure, and unchanged EXE/ZIP SHA-256 | PASS on code candidate SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9`; Release/tag publish NOT RUN | Actions run [39554220174](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39554220174); standalone EXE SHA `ccce850276056beaeecd0479ec28633bd741ca64b5faaebb615f94be7eeb425e`, portable ZIP SHA `a7805935bf50c108612c18ca8b4247610298a7336711c03833b53bf8b709a851` |
| Repository policy regression | Ubuntu · `node --test tests/*.test.cjs` | PASS · 32/32 | run 39554220174 governance job |
| Secret scan | Gitleaks action pinned commit; full history | PASS | run 39554220174 governance job; initial Windows installer 404 was fixed by moving scan to Ubuntu |
| NuGet vulnerability audit | Restore treats NU1901–NU1904 as errors; `dotnet list AiQuotaMonitor.sln package --vulnerable --include-transitive` | PASS · no vulnerable packages reported for Core, Providers, App, Tests | run 39554220174 Windows job |
| 合成資料洩漏 regression | xUnit RPC error redaction, environment allowlist, Claude statusline field allowlist and CurrentUserOnly pipe test | PASS · 56/56 | run 39554220174; TRX artifact in Windows job |
| Codex schema / protocol | Synthetic JSON-RPC and rate-limit response fixtures | PASS within 56/56 xUnit tests | run 39554220174 |
| Claude statusline parser / bridge | Official documented fields, synthetic JSON, PowerShell 5.1 parser, CurrentUserOnly pipe | PASS | run 39554220174 Windows xUnit and helper-parser steps; real Claude session remains unverified |
| Windows 11 interactive UAT | Issue #7 checklist | HUMAN_TEST_PENDING | Must be run on a Windows 11 x64 device; CI is not a substitute |

Monitor 不讀取 Codex 或 Claude credential 檔、不自動登入、不安裝或修復 CLI、不執行付費推論。Codex app-server 自行管理其帳號／連線狀態。Claude helper 只挑出官方 `five_hour` 與 `seven_day` rate-limit 欄位，透過 CurrentUserOnly pipe 傳入 Monitor；合成 session/transcript 欄位的洩漏測試通過。所有單元與 helper 整合 fixture 都是合成資料。

上述 Windows/Ubuntu 證據適用於程式候選來源 SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9`；後續文件-only head 的 Checks 以 PR #10 顯示為準。Release workflow 尚未執行，Windows 11 實機 UAT 仍為 `HUMAN_TEST_PENDING`。目前本機為受限 Linux sandbox，沒有 .NET SDK、Windows GUI 或 Windows PowerShell 5.1；未宣稱本機 Windows build、parser 或實機安全檢查通過。

## Issue #11 v0.1.2 安全驗證狀態

以下驗證適用於完整 PR head `92b68a7e0e6549ab8f2a8ab114d45f3b3d106917`，由 [Actions run 39572768848](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39572768848) 的 Windows Server 2025 與 Ubuntu GitHub-hosted runners 執行。Windows 11 UAT 與真實供應商帳號驗證仍分別 pending/not verified；合成 fixture 不代表真實額度。

| 檢查 | 風險控制 | 狀態 |
| --- | --- | --- |
| Claude setup | 啟動時不讀設定；使用者確認檢查後才讀；不可讀檔與競爭建立 fail closed；journal 先於設定寫入；既有 statusLine 不覆寫；復原拒絕覆寫被修改的狀態列 | PASS · Windows xUnit 123/123 |
| Claude helper | helper 只送合法 rate_limits 欄位或固定錯誤代碼；錯誤百分比/reset 轉安全 sentinel；malformed JSON、session ID、transcript path 不輸出、不記錄 | PASS · Windows xUnit + PowerShell 5.1 synthetic parser |
| Codex account probe | `account/read` 使用 `refreshToken=false`，只判斷 account null/object；錯誤訊息及 account identifier 不進 UI、log 或測試輸出 | PASS · synthetic JSON-RPC within Windows xUnit |
| Codex process | CLI 路徑只接受 codex.exe 候選；程序採 OS env allowlist、stderr 丟棄、短生命週期；不碰 credential 檔 | PASS · Windows xUnit |
| Diagnostics/timestamps | UI 僅用固定錯誤碼映射本地化建議；原始配額/帳號資料不入 log；失敗清除百分比且保留最後成功時間 | PASS · Windows xUnit |
| Secret scan | Gitleaks full-history | PASS · Ubuntu PR job |
| Actual vendor accounts | 本環境不讀取登入憑證，不自動登入、不推論 | NOT VERIFIED · Windows 11 UAT pending |
