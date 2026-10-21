# 安全驗證報告

報告日期：2026-10-21。每一結果僅適用於表列完整 commit SHA。後續 PR head 以 PR #8 Checks 為準；Windows 11 實機 UAT 另行標示。

| Gate | 命令／方法 | 狀態 | 證據 |
| --- | --- | --- | --- |
| Release publish token isolation | Build/package uses `contents:read`; isolated artifact-only publisher receives `contents:write`; checkout credential persistence disabled; only v0.1.0 tag contained in main may proceed | Implemented; verify on final workflow run | `.github/workflows/release.yml` |
| Repository policy regression | Ubuntu · `node --test tests/*.test.cjs` | PASS on SHA `10496a43c520b079f0f5c02d3ee71b94ecef751d`; locally BLOCKED | PR run 39538202866 governance job passed; local sandbox blocks nested processes with `EPERM` |
| Secret scan | Gitleaks GitHub Action，完整 repo history on Ubuntu | PASS on SHA `10496a43c520b079f0f5c02d3ee71b94ecef751d` | PR run 39538202866 governance job passed; first Windows attempt 39535818420 failed before scan on installer URL 404 and was moved to Ubuntu |
| NuGet vulnerability audit | Restore 時 NU1901–NU1904 為錯誤；`dotnet list AiQuotaMonitor.sln package --vulnerable --include-transitive` | NOT RUN on SHA `10496a43c520b079f0f5c02d3ee71b94ecef751d` | xUnit failure stopped before audit; final head pending |
| 合成資料洩漏 regression | `dotnet test AiQuotaMonitor.sln -c Release`; RPC error redaction and child environment allowlist tests | 52 passed / 1 failed on SHA `10496a43c520b079f0f5c02d3ee71b94ecef751d`; fix pending | The failed bridge test process timed out after 15s with an incomplete PowerShell test environment; no provider/token leakage assertion failed |
| Codex schema parser | synthetic JSON fixture tests | PASS as part of 52 passing xUnit tests on SHA `10496a43c520b079f0f5c02d3ee71b94ecef751d` | PR run 39538202866 Windows test output |
| Claude statusline allowlist/parser | synthetic JSON fixtures; PowerShell 5.1 parser/helper; CurrentUserOnly pipe test | Bridge xUnit timed out on SHA `10496a43c520b079f0f5c02d3ee71b94ecef751d`; harness fix pending. PowerShell parser step skipped | PR run 39538202866; real Claude UAT remains pending |
| Windows 11 互動 UAT | Issue #7 checklist | HUMAN_TEST_PENDING | 需 Windows 11 使用者實機 |

目前本機是 Linux 且未安裝 .NET SDK；未宣稱本機 restore/build/test、PowerShell parser、Windows GUI 或實機安全檢查通過。
