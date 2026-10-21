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
| Release build | `dotnet build AiQuotaMonitor.sln -c Release --no-restore` | PASS in PR run `39540530891` on SHA `2ff1fb7909079856cefa6ba7beee4848b09fe3bc` |
| Unit test | `dotnet test AiQuotaMonitor.sln -c Release --no-build` | PASS · 55/55 in PR run `39540530891` on SHA `2ff1fb7909079856cefa6ba7beee4848b09fe3bc`; synthetic DTO/provider/settings/bridge tests |
| Repository governance tests | `node --test tests/*.test.cjs` | PASS in Ubuntu job of PR run `39540530891`; local sandbox child-process restrictions prevented a complete local run |
| Windows PowerShell check | `tests/scripts/Test-ClaudeStatusline.ps1`，workflow `shell: powershell` | PASS in PR run `39540530891`; parses all `.ps1` and invokes helper with synthetic statusline input |
| Secret scan | Gitleaks action SHA pinned; full history on Ubuntu | PASS in PR run `39540530891` |
| Direct app NuGet dependencies | 無第三方 `PackageReference`；Core/Providers 是同 repo project reference | App `.csproj`；self-contained runtime components會出現在 Release SBOM |
| Test dependencies | Microsoft.NET.Test.Sdk 17.12.0、xunit 2.9.3、xunit.runner.visualstudio 3.0.1 | Test csproj；fixed versions，NuGet audit on restore |
| GitHub Action pins | checkout `11d596…`、setup-node `49933…`、setup-dotnet `67a357…`、upload-artifact `ea165…`、download-artifact `d3f86…`、gitleaks `ff981…`、softprops `3bb127…` | `docs/DEPENDENCIES.md`；commit SHA pins |
| Provider sources | Codex official app-server `account/rateLimits/read`; optional Claude official statusline local JSON | `docs/providers/codex-evidence.md`, `claude-evidence.md`; CLI versions UNKNOWN |
| Token / data paths | No credential file access by Monitor; Codex child receives OS env allowlist; Claude sends two rate-limit windows over current-user pipe | Provider source, security/privacy docs; app-server itself manages Codex auth/network |
| Local settings | `%LOCALAPPDATA%\AiQuotaMonitor\settings.json` | Schema 1, atomic replace, non-sensitive UI/interval/alert/startup/floating-panel prefs only; pause is session-only |
| Default refresh / timeout | 300 sec / 10 sec per provider; manual throttle 10 sec | MainViewModel and QuotaRefreshService; timeout/backoff tests pending Windows runner |
| Release | tag `v0.1.0`, Windows x64 `PublishSingleFile`, portable ZIP + standalone EXE + SHA256 + SPDX SBOM + reports | `.github/workflows/release.yml`; not yet run |
| Release state | PR #8 open; latest completed CI run `39540530891` passed on SHA `2ff1fb7909079856cefa6ba7beee4848b09fe3bc`; `v0.1.0` tag absent | User authorized the scoped prerelease after exact-head review, CI, policy recheck and merge gates |
| Windows 11 physical UAT | `HUMAN_TEST_PENDING` | `docs/UAT_WINDOWS11.md`; CI does not substitute |
| Local environment | Linux; `dotnet` not installed; no Windows GUI/PowerShell 5.1 | Command lookup; no compile/test claim |
| Current main protection snapshot | Branch payload `protected=false`, required checks `off`/empty; repo rulesets API returned `[]`; branch-protection endpoint returned 403 | Read-only GitHub API snapshot on 2026-10-21; the 403 is recorded separately, not used alone to infer policy |
| Actions quota | UNKNOWN | No quota failure reported; do not infer remaining quota from successful runs |
