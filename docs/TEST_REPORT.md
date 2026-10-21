# 測試報告

## Issue #9 / PR #10 修正驗證狀態

修正程式來源 SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9` 的 Windows Gate 已在 GitHub Actions run [39554220174](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39554220174) 通過。較早候選 `8aef7db2a25f637c8df8231b1f29fd540e9881f5` 的 run 39553296900 已由此精確候選取代。這仍是候選驗證，不是 Release；`v0.1.1` 對外發布須等待精確 PR head 的文件變更 CI、獨立 review、Squash Merge、合併後 tag Release workflow 與同一最終產物驗證。此報告記錄 run 39554220174 實際驗證的程式碼來源 SHA；後續文件-only提交的 Actions 結果以 PR Checks 為準。

### Issue #9 packaged EXE Gate 結果

| 檢查 | 結果 | 證據 |
| --- | --- | --- |
| Windows restore / Release build | PASS | GitHub Windows Server 2025 runner；run 39554220174；Source SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9` |
| XAML regression / xUnit | PASS · 56 passed / 0 failed / 0 skipped | 同一 Windows job；唯讀 ProgressValue XAML regression test 已執行 |
| PowerShell scripts parse、Claude synthetic helper、NuGet audit | PASS | 同一 Windows job；完整 PowerShell scripts parse，NuGet vulnerability audit 成功 |
| 最終 portable ZIP 內 standalone EXE smoke | PASS | 最終 package 完成後啟動該 EXE；offline/no-account/no-token 模式跳過 provider/CLI 刷新 |
| UI ready / 穩定存活 / clean close | PASS | 可見主視窗 `AI 額度監控器`、Claude/Codex 兩卡、四條 OneWay progress binding；外部視窗 handle/title 保持 5 秒，正常關閉 |
| 異常與崩潰事件 | PASS · 0 個相符事件 | smoke exception marker 與 Windows Application Error 1000 / .NET Runtime 1026 檢查 |
| 故意啟動失敗反向案例 | PASS | 注入 `System.Windows.Markup.XamlParseException`，程序非零退出且未寫 UI ready marker；相同 Gate 拒絕壞版 |
| SHA-256 同一性 | PASS | EXE `ccce850276056beaeecd0479ec28633bd741ca64b5faaebb615f94be7eeb425e`；portable ZIP `a7805935bf50c108612c18ca8b4247610298a7336711c03833b53bf8b709a851`；zip 內 EXE 與被測獨立 EXE 相同 |
| 候選證據 artifact | 已上傳 | Actions run artifact `windows-packaged-smoke-evidence`，ID `12324264835`，artifact digest `sha256:1598a434617bd7ca6efab40f1d6e37655fed518abe32f7392120ba83a12e074b`，保留至 2026-11-04 |
| Gitleaks / Ubuntu governance | PASS | 同一 PR run；repository-governance job 成功 |
| Release / Windows 11 實機 UAT | 尚未發布 / HUMAN_TEST_PENDING | Release 需合併後另跑 tag workflow；Server 2025 Runner 不等同 Windows 11 裝置 UAT |

Smoke command: `tests/scripts/Test-WindowsExecutableSmoke.ps1 -ExecutablePath <packaged EXE> -PackagePath <portable ZIP> -VerifyNegativeControl`；run 39554220174 使用同一個 Windows build/package artifact 執行正向與負向 Gate、再核對 EXE/ZIP SHA。Evidence 只包含 runner、source SHA、UI 結果、例外型別與檔案 hash，不含帳號、Token 或額度資料。

## Issue #11 / v0.1.2 額度來源修正

PR head `dcd15329bbb83393d496ec3096276cd6e649c0c2` 已於 Windows Server 2025 與 Ubuntu GitHub-hosted runners 完成可重現驗證：[Actions run 39573823707](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39573823707)。Windows job restore/build、123/123 xUnit、Claude helper 合成輸入及 PowerShell script parse、dependency audit、最終 packaged EXE smoke、故意啟動失敗反向測試和同一 EXE/ZIP SHA-256 檢查均通過；Ubuntu governance/Gitleaks 通過。

| 最終 Windows 產物 Gate | 結果 | 安全證據 |
| --- | --- | --- |
| 最終 EXE GUI 啟動與 ready 狀態 | PASS | Main window/UI ready、offline/no-account/no-token 隔離啟動；程序至少存活 5 秒；無匹配未處理崩潰事件 |
| 故意失敗反向測試 | PASS | 注入 `XamlParseException` 後未寫 UI ready；Gate 判定失敗啟動 |
| xUnit / PowerShell helper | PASS | Windows xUnit 123/123；helper 測試僅使用合成 statusline JSON；所有 PowerShell scripts 可解析 |
| 同一產物 SHA-256 | PASS | standalone EXE `8d4ec404a422ac2961c48867469c04a55522024b175793d802b21b1a58083907`；portable ZIP `619d65c42458e55bf6cceedf8e254042b91f4f599662247c67a13c3ae37e8761` |
| Runner evidence artifact | 已上傳 | `windows-packaged-smoke-evidence`，artifact ID `12334145341`；不含帳號、Token、真實配額或供應商原始回應 |

- 真實供應商資料：`NOT VERIFIED`。未在此環境啟動真實 Claude Code/Codex 帳號、登入、讀取 Token 或執行推論；合成百分比不是實際額度。
- Windows 11 實機 UAT：`HUMAN_TEST_PENDING`。上述 runner 是 Windows Server 2025，不能代替 Windows 11 裝置；請按 `docs/UAT_WINDOWS11.md` 驗證真實 statusline 輸入與 Codex Desktop/CLI 版本，公開紀錄不得包含帳號或額度原始資料。
- 本機 Linux 不具備 `dotnet`、`pwsh` 或 GUI，因此未宣稱本機 build 或 Windows GUI 測試。

更新日期：2026-10-21。下列自動驗證適用於完整來源 SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f`，PR workflow run [39540874352](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39540874352)。Windows job 使用 GitHub Windows Server 2025 runner，不是 Windows 11 使用者裝置。

## 自動驗證

| 檢查 | 環境／命令 | 結果 | 證據 |
| --- | --- | --- | --- |
| Repository governance | Ubuntu · `node --test tests/*.test.cjs` | PASS · 32/32 | run 39540874352 `repository-governance` job |
| Gitleaks | Ubuntu GitHub Actions，full history | PASS | run 39540874352 `repository-governance` job |
| Restore | Windows · `dotnet restore AiQuotaMonitor.sln` | PASS | run 39540874352 `build-and-test` job |
| Release build | Windows · `dotnet build AiQuotaMonitor.sln -c Release --no-restore` | PASS | run 39540874352 `build-and-test` job |
| xUnit | Windows · `dotnet test AiQuotaMonitor.sln -c Release --no-build` | PASS · 55 passed / 0 failed | run 39540874352; TRX artifact `windows-test-report` |
| Claude PowerShell helper | Windows PowerShell 5.1 parser and synthetic input | PASS | `Test-ClaudeStatusline.ps1` step in run 39540874352 |
| NuGet vulnerability audit | Windows · `dotnet list AiQuotaMonitor.sln package --vulnerable --include-transitive` | PASS · all four projects reported no vulnerable packages | run 39540874352 `List dependency audit` step |
| Windows x64 publish / release assets | `v0.1.1` release workflow | NOT RUN | awaits exact PR head checks, independent review, merge, then Windows smoke/hash workflow |
| Windows 11 device UAT | `docs/UAT_WINDOWS11.md` | HUMAN_TEST_PENDING | no Windows 11 physical device was available |

This run includes the isolated PowerShell JSON probe's terminating-error handling. The report records the tested source SHA; check PR #8 Checks for any later documentation-only head validation.

The Release build completed with 0 errors and 3 analyzer warnings: two `WFAC010` warnings concerning the app manifest's high-DPI declarations and one `xUnit2010` assertion-style warning. High-DPI behavior still needs the Windows 11 display-scaling UAT in `docs/UAT_WINDOWS11.md`.

## Earlier issues repaired

- Gitleaks' Windows installer URL returned HTTP 404 before scanning; the pinned scan now runs in the Ubuntu governance job.
- Windows compilation and test-compilation failures were fixed and covered by the later successful build.
- PowerShell bridge tests initially timed out because implicit `Microsoft.PowerShell.Utility` command discovery stalled in the restricted child environment. A same-environment probe isolated module loading; the helper now imports the module explicitly, and the real CurrentUserOnly pipe test passes.

## 本機環境限制

開發 checkout 位於受限 Linux sandbox，沒有 `dotnet`、Windows GUI 或 Windows PowerShell 5.1。Node governance suite 的巢狀程序測試在本機被 sandbox 以 `EPERM` 阻止；Ubuntu CI 已通過完整 suite。本機 XML/YAML/JSON parse 是靜態檢查，不代表 build、test、PowerShell、WPF 執行或 Windows 11 UAT。未執行實際 Codex CLI、Claude Code session 或付費推論。

## 自動化測試涵蓋

- DTO 百分比界限、NaN／Infinity、null 值與 used/remaining 映射。
- 單一來源失敗隔離、每來源 timeout、呼叫者 cancellation。
- Codex 五小時／每週 duration mapping、weekly-only、0／100%、缺少／毫秒 reset、錯誤百分比、未知 duration、錯誤 bucket、重複視窗和格式錯誤。
- Codex JSONL notification／response id correlation、RPC 錯誤訊息不外洩、CLI 未安裝／RPC 不支援／失敗狀態、子程序環境變數 allowlist。
- Claude 官方 statusline 欄位、百分比/reset 驗證、缺值狀態立即清除前一筆數值與 15 分鐘過期處理；Windows integration 以合成輸入驗證 CurrentUserOnly pipe allowlist、未知快照清除及 stdin 子程序環境。
- 設定 JSON atomic save、壞檔／未知 schema fallback、敏感欄位不存在、有效值範圍與浮動面板置頂偏好。
- 監控暫停政策：停止排程與 lifecycle refresh、禁止低額提醒、仍允許手動刷新；對應 tray／resume 實機項目在 UAT #16。

自動測試只使用合成 JSON，不接觸真實 Token、帳號、供應商網路或付費推論。CI PASS 不替代 Windows 11 互動 UAT。
