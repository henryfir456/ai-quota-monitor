# 測試報告

## Issue #9 / PR #10 修正驗證狀態

修正程式來源 SHA `8aef7db2a25f637c8df8231b1f29fd540e9881f5` 的 Windows Gate 已在 GitHub Actions run [39553296900](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39553296900) 通過。較早候選 `9a3d6ddeeaacfc213d4523a1f502769397f0df09` 的 run 39552400463 已由此精確候選取代。這仍是候選驗證，不是 Release；`v0.1.1` 對外發布須等待精確 PR head 的文件變更 CI、獨立 review、Squash Merge、合併後 tag Release workflow 與同一最終產物驗證。此報告記錄 run 39553296900 實際驗證的程式碼來源 SHA；文件-only 後續提交由該 PR head Checks 另行驗證。

### Issue #9 packaged EXE Gate 結果

| 檢查 | 結果 | 證據 |
| --- | --- | --- |
| Windows restore / Release build | PASS | GitHub Windows Server 2025 runner；run 39553296900；Source SHA `8aef7db2a25f637c8df8231b1f29fd540e9881f5` |
| XAML regression / xUnit | PASS · 56 passed / 0 failed / 0 skipped | 同一 Windows job；唯讀 ProgressValue XAML regression test 已執行 |
| PowerShell scripts parse、Claude synthetic helper、NuGet audit | PASS | 同一 Windows job；完整 PowerShell scripts parse，NuGet vulnerability audit 成功 |
| 最終 portable ZIP 內 standalone EXE smoke | PASS | 最終 package 完成後啟動該 EXE；offline/no-account/no-token 模式跳過 provider/CLI 刷新 |
| UI ready / 穩定存活 / clean close | PASS | 可見主視窗 `AI 額度監控器`、Claude/Codex 兩卡、四條 OneWay progress binding；外部視窗 handle/title 保持 5 秒，正常關閉 |
| 異常與崩潰事件 | PASS · 0 個相符事件 | smoke exception marker 與 Windows Application Error 1000 / .NET Runtime 1026 檢查 |
| 故意啟動失敗反向案例 | PASS | 注入 `System.Windows.Markup.XamlParseException`，程序非零退出且未寫 UI ready marker；相同 Gate 拒絕壞版 |
| SHA-256 同一性 | PASS | EXE `fd03e4d6ee9357e67bc3248a98840b39eee058a2911efd0bd55e538d1021282b`；portable ZIP `c72e1a80fdc4ee0bd2100654eb75b316fb5ef48705548a37471c2918f1540ba9`；zip 內 EXE 與被測獨立 EXE 相同 |
| 候選證據 artifact | 已上傳 | Actions run artifact `windows-packaged-smoke-evidence`，ID `12324592523`，artifact digest `sha256:8df9aaa4aa73a059dc82be8521050c2eecd183d43168663ff122c0e94103a56c`，保留至 2026-11-04 |
| Gitleaks / Ubuntu governance | PASS | 同一 PR run；repository-governance job 成功 |
| Release / Windows 11 實機 UAT | 尚未發布 / HUMAN_TEST_PENDING | Release 需合併後另跑 tag workflow；Server 2025 Runner 不等同 Windows 11 裝置 UAT |

Smoke command: `tests/scripts/Test-WindowsExecutableSmoke.ps1 -ExecutablePath <packaged EXE> -PackagePath <portable ZIP> -VerifyNegativeControl`；run 39553296900 使用同一個 Windows build/package artifact 執行正向與負向 Gate、再核對 EXE/ZIP SHA。Evidence 只包含 runner、source SHA、UI 結果、例外型別與檔案 hash，不含帳號、Token 或額度資料。

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
| Windows x64 publish / release assets | `v0.1.1` release workflow | NOT RUN | awaits same-head PR gates, review, merge, then Windows smoke/hash workflow |
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
