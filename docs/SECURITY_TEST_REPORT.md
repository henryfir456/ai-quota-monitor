# 安全驗證報告

報告日期：2026-10-21。驗證只在 PR head 的 Windows CI 執行後填入 run URL、commit SHA 與實際結果；下列預期項目尚未取得該 run 證據時不得改成 PASS。

| Gate | 命令／方法 | 狀態 | 證據 |
| --- | --- | --- | --- |
| Repository policy regression | Ubuntu · `node --test tests/*.test.cjs` | BLOCKED locally; CI pending | Sandbox prevented nested process execution (`EPERM`); CI job added |
| Secret scan | Gitleaks GitHub Action，完整 repo history on Ubuntu | FAILED on initial Windows attempt; new Ubuntu run pending | Run 39535818420 hit Windows installer URL 404 before scanning; moved to Ubuntu job |
| NuGet vulnerability audit | `dotnet restore AiQuotaMonitor.sln`，NU1901–NU1904 視為錯誤 | NOT RUN | 等 Windows Actions PR run |
| 合成資料洩漏 regression | `dotnet test AiQuotaMonitor.sln -c Release`；RPC error message 與子程序環境 allowlist 測試只含 synthetic values | NOT RUN | 等 Windows Actions PR run |
| Codex schema parser | synthetic JSON fixture tests | NOT RUN | 等 Windows Actions PR run |
| Claude statusline allowlist/parser | synthetic JSON fixture tests；Windows PowerShell 5.1 parser、helper execution 與 CurrentUserOnly pipe test | NOT RUN | 等 Windows CI；另有真實 Claude UAT |
| Windows 11 互動 UAT | Issue #7 checklist | HUMAN_TEST_PENDING | 需 Windows 11 使用者實機 |

目前本機是 Linux 且未安裝 .NET SDK；未宣稱本機 restore/build/test、PowerShell parser、Windows GUI 或實機安全檢查通過。
