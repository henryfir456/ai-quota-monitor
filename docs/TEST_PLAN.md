# TEST_PLAN｜驗證計畫

Issue #9 / PR #10 新增 Gate 已在程式來源 SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9` 由 Windows Actions run [39554220174](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39554220174) 實際執行並通過。以下舊 SHA `24e31940...` 列只屬於 v0.1.0 歷史；文件-only 後續提交的最新狀態以 PR #10 對應 head Checks 為準。

| 領域 | 驗證方法 | 狀態 | 證據 |
| --- | --- | --- | --- |
| .NET restore/NuGet audit | Windows runner · `dotnet restore AiQuotaMonitor.sln`; `dotnet list AiQuotaMonitor.sln package --vulnerable --include-transitive` | PASS on SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f` | PR run [39540874352](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39540874352): restore and audit passed; no vulnerable packages reported for all four projects |
| Repository governance tests | Ubuntu runner · `node --test tests/*.test.cjs` | PASS · 32/32 on SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f`; local BLOCKED | Run 39540874352 governance job passed; local sandbox blocks nested Node processes with `EPERM` |
| Release build | Windows runner · `dotnet build AiQuotaMonitor.sln -c Release --no-restore` | PASS on SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f` | PR run 39540874352 |
| xUnit | Windows runner · `dotnet test AiQuotaMonitor.sln -c Release --no-build` | PASS · 55/55 on SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f` | PR run 39540874352; GitHub Windows Server 2025 runner |
| Codex schema / RPC | synthetic fixture parser, timeout, unsupported state, environment allowlist and error redaction tests | PASS within 55/55 xUnit tests | PR run 39540874352 |
| Claude parser / pipe | official statusline field synthetic fixture; Windows PowerShell 5.1 parser; CurrentUserOnly bridge integration with synthetic session markers | PASS | xUnit and `Test-ClaudeStatusline.ps1` passed in PR run 39540874352; no real Claude session was started |
| Settings | schema fallback, atomic write, safe default/range, floating-panel topmost preference and no credential fields | PASS within 55/55 xUnit tests | PR run 39540874352 |
| Monitoring pause | paused scheduled/lifecycle refresh, alert suppression, timer scheduling, manual-refresh allowance and resume behavior | PASS within 55/55 xUnit tests; tray interaction pending UAT | PR run 39540874352; tray interaction in UAT #16 |
| Secret scan | Gitleaks action pinned commit, full history | PASS on SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f` | PR run 39540874352 governance job |
| WPF/XAML / WinForms / self-contained publish | Windows Desktop SDK build and v0.1.1 tag publish | Build PASS in run 39554220174; v0.1.1 tag publish NOT RUN | Release workflow runs after PR #10 review/merge and the authorized v0.1.1 tag |
| Issue #9 ProgressValue Binding regression | Windows xUnit parses all app XAML resources and asserts every `ProgressValue` binding is `Mode=OneWay` | PASS · run 39554220174 · source SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9` | Windows xUnit report: 56 passed / 0 failed |
| Final packaged EXE positive startup | GitHub Windows Runner · visible window/title, UI ready JSON, two cards, four OneWay progress bars, alive for bounded 5 seconds, clean close | PASS · run 39554220174 | `.github/workflows/ci.yml`; run URL above; no Linux equivalent is claimed |
| Unhandled exception / Windows crash events | Same Windows packaged EXE · exception marker plus Application Error 1000 / .NET Runtime 1026 query | PASS · 0 matching events | Sanitized smoke evidence in artifact 12324264835 |
| Offline / no-account / no-token startup | Windows packaged EXE · isolated settings path, common credential environment keys removed, smoke mode skips Provider/CLI calls | PASS | No OAuth Token is read, logged or sent; hosted Runner is not Windows 11 device UAT |
| Fault-injected negative startup | Same EXE · intentional `XamlParseException` exits nonzero before ready marker; job fails if it is not rejected | PASS · rejected by same gate | Run 39554220174; `--smoke-test-force-failure` plus `-VerifyNegativeControl` |
| Package identity / SHA-256 | Windows · standalone EXE hash equals smoke evidence and ZIP embedded EXE; ZIP hash unchanged during smoke; publisher rechecks manifest | PASS · EXE `ccce850276056beaeecd0479ec28633bd741ca64b5faaebb615f94be7eeb425e`; ZIP `a7805935bf50c108612c18ca8b4247610298a7336711c03833b53bf8b709a851` | `Complete-ReleaseArtifacts.ps1`; artifact `windows-packaged-smoke-evidence` ID 12324264835; tag publisher run remains pending |
| Windows 11 x64 GUI / process UAT | `docs/UAT_WINDOWS11.md` | HUMAN_TEST_PENDING | no Windows 11 device available in current environment |
| Provider real-client matrix | real Codex CLI/Desktop version and user-configured Claude statusline | NOT VERIFIED | target Windows application versions not established; no CLI/session started |

本機已執行 Python XML/YAML/JSON 靜態 parse，但不代表 C# compile。Linux sandbox 沒有 .NET SDK、Windows PowerShell 5.1 或 Windows GUI；也禁止以 GitHub Windows Server runner 取代 Windows 11 實機 UAT。

狀態詞彙：`PASS`、`FAIL`、`NOT RUN`、`NOT VERIFIED`、`HUMAN_TEST_PENDING`。最新 PR head 與本表所列已完成驗證 SHA 不同時，PR Checks 是該 head 的權威結果。

## Issue #11 v0.1.2 本輪測試

下列測試使用官方 protocol 形狀和合成回應；PR head `dcd15329bbb83393d496ec3096276cd6e649c0c2` 的 Windows/Ubuntu Actions run [39573823707](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39573823707) 全部通過。它們不證明真實帳號成功取得額度。

| Provider / 狀態 | 測試方式 | 預期 | 狀態 |
| --- | --- | --- | --- |
| Claude success | 官方 `rate_limits.five_hour` / `seven_day` 合成 JSON，透過 PowerShell helper 與 CurrentUserOnly pipe | 只傳百分比/reset，UI 新增成功時間；測試資料清楚標示合成 | PASS · 123/123 Windows xUnit + PowerShell helper |
| Claude unconfigured / uninstalled | provider 初始狀態與 executable locator | 分別顯示未設定、找不到 CLI；不啟動 Claude | PASS · Windows xUnit |
| Claude parse failure / unsupported shape | malformed JSON 與非 object JSON 經 helper/parser | 顯示解析失敗或未支援；不外洩原文、不保留數字 | PASS · Windows xUnit + PowerShell 5.1 |
| Claude no-input timeout / old success | 設定後超過 15 分鐘無 pipe 輸入；成功資料超過 freshness window | 前者顯示逾時；後者顯示過期並保留最後成功時間 | PASS · Windows xUnit |
| Claude settings configure / conflict / rollback | 暫存使用者 profile、合成 `settings.json` 與 Windows ACL | 啟動時不讀設定；同意檢查後讀取；不可讀檔不能誤判為不存在；journal 先於設定寫入；新檔 no-overwrite；衝突保留其他設定；helper 使用 PS 5.1 可讀 BOM | PASS · Windows xUnit |
| Codex success | 官方 initialize/account/read/rateLimits JSONL 合成回應 | `experimentalApi=true`、`refreshToken=false`、期限映射正確；回應不保留帳號識別 | PASS · Windows xUnit |
| Codex uninstalled / not logged in | locator no-match 與 `account:null` 合成 RPC | 顯示未安裝／未登入並提供建議；不嘗試登入或額度推測 | PASS · Windows xUnit |
| Codex parse failure / timeout / unsupported | malformed response、provider timeout、RPC -32601、未知窗口 schema | 各自顯示明確狀態；百分比保持 N/A | PASS · Windows xUnit |
| Refresh timestamp and UI state | success 後接失敗快照並監聽 ViewModel property change | 失敗不保留舊百分比；最後成功時間保留且診斷即時更新 | PASS · Windows xUnit |
| Final v0.1.2 packaged EXE | Windows final package GUI smoke、reverse control 與 SHA identity | offline/no-account/no-token 啟動 PASS；失敗或 UNKNOWN 阻止 Release | PASS · Windows Server 2025; EXE `8d4ec404a422ac2961c48867469c04a55522024b175793d802b21b1a58083907`; ZIP `619d65c42458e55bf6cceedf8e254042b91f4f599662247c67a13c3ae37e8761` |
| Real provider accounts | Windows 11 真實 Claude Code/Codex 版本與登入狀態 | 只有實際來源回報欄位後才能記錄額度讀取成功 | HUMAN_TEST_PENDING |
