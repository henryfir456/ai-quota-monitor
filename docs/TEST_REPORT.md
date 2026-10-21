# 測試報告

更新日期：2026-10-21。下列自動驗證適用於完整 SHA `2ff1fb7909079856cefa6ba7beee4848b09fe3bc`，PR workflow run [39540530891](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39540530891)。該 Windows runner 是 GitHub `windows-latest`，不是 Windows 11 使用者裝置。

## 自動驗證

| 檢查 | 環境／命令 | 結果 | 證據 |
| --- | --- | --- | --- |
| Repository governance | Ubuntu · `node --test tests/*.test.cjs` | PASS | run 39540530891 `repository-governance` job |
| Gitleaks | Ubuntu GitHub Actions，full history | PASS | run 39540530891 `repository-governance` job |
| Restore | Windows · `dotnet restore AiQuotaMonitor.sln` | PASS | run 39540530891 `build-and-test` job |
| Release build | Windows · `dotnet build AiQuotaMonitor.sln -c Release --no-restore` | PASS | run 39540530891 `build-and-test` job |
| xUnit | Windows · `dotnet test AiQuotaMonitor.sln -c Release --no-build` | PASS · 55 passed / 0 failed | run 39540530891; TRX artifact `windows-test-report` |
| Claude PowerShell helper | Windows PowerShell 5.1 parser and synthetic input | PASS | `Test-ClaudeStatusline.ps1` step in run 39540530891 |
| NuGet vulnerability audit | Windows · `dotnet list AiQuotaMonitor.sln package --vulnerable --include-transitive` | PASS · all four projects reported no vulnerable packages | run 39540530891 `List dependency audit` step |
| Windows x64 publish / release assets | `v0.1.0` release workflow | NOT RUN | awaits reviewed PR merge and authorized prerelease tag |
| Windows 11 device UAT | `docs/UAT_WINDOWS11.md` | HUMAN_TEST_PENDING | no Windows 11 physical device was available |

The next PR head strengthens the isolated PowerShell JSON probe with terminating-error handling; its exact-head workflow remains pending. Do not treat this SHA as evidence for a later head.

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
