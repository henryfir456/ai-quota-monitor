# TEST_PLAN｜驗證計畫

Issue #9 / PR #10 的下列新 Gate 尚未取得 Windows Actions 結果。舊 SHA `24e31940...` 的驗證只屬於 v0.1.0 歷史，不涵蓋本次修正。

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
| WPF/XAML / WinForms / self-contained publish | Windows Desktop SDK build and v0.1.0 tag publish | Build PASS; publish NOT RUN | Release workflow runs only after reviewed v0.1.0 tag |
| Issue #9 ProgressValue Binding regression | Windows xUnit parses all app XAML resources and asserts every `ProgressValue` binding is `Mode=OneWay` | PASS · run 39552400463 · source SHA `9a3d6ddeeaacfc213d4523a1f502769397f0df09` | Windows xUnit report: 56 passed / 0 failed |
| Final packaged EXE positive startup | GitHub Windows Runner · visible window/title, UI ready JSON, two cards, four OneWay progress bars, alive for bounded 5 seconds, clean close | PASS · run 39552400463 | `.github/workflows/ci.yml`; run URL above; no Linux equivalent is claimed |
| Unhandled exception / Windows crash events | Same Windows packaged EXE · exception marker plus Application Error 1000 / .NET Runtime 1026 query | PASS · 0 matching events | Sanitized smoke evidence in artifact 12323623137 |
| Offline / no-account / no-token startup | Windows packaged EXE · isolated settings path, common credential environment keys removed, smoke mode skips Provider/CLI calls | PASS | No OAuth Token is read, logged or sent; hosted Runner is not Windows 11 device UAT |
| Fault-injected negative startup | Same EXE · intentional `XamlParseException` exits nonzero before ready marker; job fails if it is not rejected | PASS · rejected by same gate | Run 39552400463; `--smoke-test-force-failure` plus `-VerifyNegativeControl` |
| Package identity / SHA-256 | Windows · standalone EXE hash equals smoke evidence and ZIP embedded EXE; ZIP hash unchanged during smoke; publisher rechecks manifest | PASS · EXE `62a14de36d35039057420af173e9c820b4ab2b2f1a7fcfe2fe6315603672d969`; ZIP `ac0a241c12427be43bebfa3f5ccbae3d60a1859f9c64d53e49d0e2be03947dd0` | `Complete-ReleaseArtifacts.ps1`; artifact `windows-packaged-smoke-evidence` ID 12323623137; tag publisher run remains pending |
| Windows 11 x64 GUI / process UAT | `docs/UAT_WINDOWS11.md` | HUMAN_TEST_PENDING | no Windows 11 device available in current environment |
| Provider real-client matrix | real Codex CLI/Desktop version and user-configured Claude statusline | NOT VERIFIED | target Windows application versions not established; no CLI/session started |

本機已執行 Python XML/YAML/JSON 靜態 parse，但不代表 C# compile。Linux sandbox 沒有 .NET SDK、Windows PowerShell 5.1 或 Windows GUI；也禁止以 GitHub Windows Server runner 取代 Windows 11 實機 UAT。

狀態詞彙：`PASS`、`FAIL`、`NOT RUN`、`NOT VERIFIED`、`HUMAN_TEST_PENDING`。最新 PR head 與本表所列已完成驗證 SHA 不同時，PR Checks 是該 head 的權威結果。
