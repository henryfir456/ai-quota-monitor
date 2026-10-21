# REPO_FACTS｜可驗證的儲存庫事實

更新日期：2026-10-21。未知能力明確標記；提交／CI／Release 結果以實際 GitHub evidence 更新。

| 欄位 | 已驗證值 | 證據／限制 |
| --- | --- | --- |
| Repository / default branch | `henryfir456/ai-quota-monitor` / `main` | Git remote、GitHub metadata；初始 SHA `7349aa0` |
| 技術棧 | .NET 8、C#、WPF/MVVM、Windows Forms NotifyIcon | `AiQuotaMonitor.sln`、App csproj；需 Windows Desktop SDK |
| 目標 | Windows 11 x64、asInvoker、self-contained portable | Issues #1/#7、app.manifest、release workflow；真機 UAT 未執行 |
| SDK | 8.0.408 + same feature band latest patch | `global.json`；當前 Linux 無 dotnet，無本地 build 證據 |
| Solution | `AiQuotaMonitor.sln` | repo root |
| Restore | `dotnet restore AiQuotaMonitor.sln` | Windows workflow；NuGet audit NU1901–NU1904 作錯誤 |
| Release build | `dotnet build AiQuotaMonitor.sln -c Release --no-restore` | PASS in Issue #9 PR run `39554220174` on source SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9`; Windows Server 2025 hosted runner |
| Unit test | `dotnet test AiQuotaMonitor.sln -c Release --no-build` | PASS · 56/56 in PR run `39554220174`; includes readonly ProgressValue XAML regression test |
| Repository governance tests | `node --test tests/*.test.cjs` | PASS · 32/32 in Ubuntu job of PR run `39554220174`; local sandbox child-process restrictions prevented a complete local run |
| Windows PowerShell check | `tests/scripts/Test-ClaudeStatusline.ps1` plus parser over every `.ps1` | PASS in PR run `39554220174`; synthetic statusline input only |
| Secret scan | Gitleaks action SHA pinned; full history on Ubuntu | PASS · no leaks found in PR run `39554220174` |
| Direct app NuGet dependencies | 無第三方 `PackageReference`；Core/Providers 是同 repo project reference | App `.csproj`；self-contained runtime components會出現在 Release SBOM |
| Test dependencies | Microsoft.NET.Test.Sdk 17.12.0、xunit 2.9.3、xunit.runner.visualstudio 3.0.1 | Test csproj；fixed versions，NuGet audit on restore |
| GitHub Action pins | checkout `11d596…`、setup-node `49933…`、setup-dotnet `67a357…`、upload-artifact `ea165…`、download-artifact `d3f86…`、gitleaks `ff981…`、softprops `3bb127…` | `docs/DEPENDENCIES.md`；commit SHA pins |
| Provider sources | Codex official app-server initialize (`experimentalApi=true`), `account/read` (`refreshToken=false`), and `account/rateLimits/read`; Claude official statusline local JSON | `docs/providers/codex-evidence.md`, `claude-evidence.md`; installed CLI/Desktop versions and real account results UNKNOWN; locator covers CODEX_HOME standalone/app-server package layouts |
| Token / data paths | No OAuth Token or credential-file access; Codex child receives OS env allowlist and only a boolean login-state decision is retained; Claude helper sends only valid quota fields or fixed diagnostic codes | Provider source, security/privacy docs; Claude settings are not read at startup and are inspected only after explicit user consent; if setup proceeds, the full file is backed up locally to preserve other settings; Codex app-server itself manages auth/network |
| Local settings | `%LOCALAPPDATA%\AiQuotaMonitor\settings.json` | Schema 1, atomic replace, non-sensitive UI/interval/alert/startup/floating-panel prefs only; pause is session-only |
| Default refresh / timeout | 300 sec / 10 sec per provider; manual throttle 10 sec | MainViewModel and QuotaRefreshService; timeout/backoff tests passed in 55/55 Windows xUnit suite |
| Current app candidate version | `0.1.2` | Issue #11 PR #12; code candidate Windows packaged EXE and synthetic tests pass in run `39573823707`; not released; true-account/Windows 11 UAT pending |
| Original Release | `v0.1.0` Windows x64 prerelease, standalone EXE + portable ZIP + SHA256 + SPDX SBOM + reports | Existing tag/assets remain unchanged; user confirmed startup issue is tracked by Issue #9 |
| Startup regression | `ProgressBar.Value` targeted readonly `QuotaWindowViewModel.ProgressValue` with default TwoWay binding | PR #10 sets ProgressBar and related DataTrigger bindings to OneWay; regression test PASS in Windows xUnit run `39554220174` |
| Windows startup Gate | Final packaged EXE smoke + intentional `XamlParseException` negative control + hash identity verification | PASS on source SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9`; EXE `ccce850276056beaeecd0479ec28633bd741ca64b5faaebb615f94be7eeb425e`, ZIP `a7805935bf50c108612c18ca8b4247610298a7336711c03833b53bf8b709a851`; artifact ID `12324264835`, run `39554220174` |
| Target Release | `v0.1.2`, Windows x64 portable ZIP + standalone EXE + SHA256 + SPDX SBOM + report/evidence | `.github/workflows/release.yml`; fixed `main` workflow_dispatch only; final publisher creates or safely resumes the tag at the exact tested source SHA only after packaged Windows smoke and hash verification pass |
| Release authorization | Current user explicitly authorized Issue #11 implementation, PR, independent review, Squash Merge after gates and `v0.1.2` prerelease if every executable gate passes | One-time authorization in current task; not standing authorization |
| Windows 11 physical UAT | `HUMAN_TEST_PENDING` | `docs/UAT_WINDOWS11.md`; CI does not substitute |
| Local environment | Linux; `dotnet` and `pwsh` not installed; no Windows GUI/PowerShell 5.1 | Command lookup; no local C# build, xUnit, or Windows GUI test claim |
| Current main protection snapshot | Branch payload `protected=false`, required checks `off`/empty; repo rulesets API returned `[]`; branch-protection endpoint returned 403 | Read-only GitHub API snapshot on 2026-10-21; the 403 is recorded separately, not used alone to infer policy |
| Actions quota | UNKNOWN | No quota failure reported; do not infer remaining quota from successful runs |
