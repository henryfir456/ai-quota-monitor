# 安全驗證報告

報告日期：2026-10-21。每一結果僅適用於表列完整 commit SHA。後續 PR head 以 PR #8 Checks 為準；Windows 11 實機 UAT 另行標示。

| Gate | 命令／方法 | 狀態 | 證據 |
| --- | --- | --- | --- |
| Release publish token isolation | Build/package uses `contents:read`; isolated artifact-only publisher receives `contents:write`; checkout credential persistence disabled; only v0.1.0 tag contained in main may proceed | Implemented; verify on final workflow run | `.github/workflows/release.yml` |
| Repository policy regression | Ubuntu · `node --test tests/*.test.cjs` | PASS on SHA `7645556fa185c7052d5986efe097d0346cb0c7d5`; locally BLOCKED | PR run 39537063633 governance job passed; local sandbox blocks nested processes with `EPERM` |
| Secret scan | Gitleaks GitHub Action，完整 repo history on Ubuntu | PASS on SHA `7645556fa185c7052d5986efe097d0346cb0c7d5` | PR run 39537063633 governance job passed; first Windows attempt 39535818420 failed before scan on installer URL 404 and was moved to Ubuntu |
| NuGet vulnerability audit | Restore 時 NU1901–NU1904 為錯誤；`dotnet list AiQuotaMonitor.sln package --vulnerable --include-transitive` | NOT RUN on SHA `7645556fa185c7052d5986efe097d0346cb0c7d5` | Windows build failed before audit; final head pending |
| 合成資料洩漏 regression | `dotnet test AiQuotaMonitor.sln -c Release`; RPC error redaction and child environment allowlist tests | NOT RUN on SHA `7645556fa185c7052d5986efe097d0346cb0c7d5` | Build failure skipped unit tests; final head pending |
| Codex schema parser | synthetic JSON fixture tests | NOT RUN on SHA `7645556fa185c7052d5986efe097d0346cb0c7d5` | Windows xUnit skipped after build failure |
| Claude statusline allowlist/parser | synthetic JSON fixtures; PowerShell 5.1 parser/helper; CurrentUserOnly pipe test | NOT RUN on SHA `7645556fa185c7052d5986efe097d0346cb0c7d5` | Windows build failure skipped these tests; parser fix is pending CI; real Claude UAT remains pending |
| Windows 11 互動 UAT | Issue #7 checklist | HUMAN_TEST_PENDING | 需 Windows 11 使用者實機 |

目前本機是 Linux 且未安裝 .NET SDK；未宣稱本機 restore/build/test、PowerShell parser、Windows GUI 或實機安全檢查通過。
