# 安全驗證報告

報告日期：2026-10-21。下列自動驗證證據適用於完整來源 SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f`，PR run [39540874352](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39540874352)。Windows job 執行於 GitHub Windows Server 2025；Windows 11 實機結果未推定。

| Gate | 命令／方法 | 狀態 | 證據 |
| --- | --- | --- | --- |
| Release publish token isolation | Build/package uses `contents:read`; isolated artifact-only publisher gets `contents:write`; checkout credential persistence disabled; only v0.1.0 tag already contained in main may proceed | Workflow policy implemented; release workflow NOT RUN | `.github/workflows/release.yml`; verify through release run |
| Repository policy regression | Ubuntu · `node --test tests/*.test.cjs` | PASS · 32/32 | run 39540874352 governance job |
| Secret scan | Gitleaks action pinned commit; full history | PASS | run 39540874352 governance job; initial Windows installer 404 was fixed by moving scan to Ubuntu |
| NuGet vulnerability audit | Restore treats NU1901–NU1904 as errors; `dotnet list AiQuotaMonitor.sln package --vulnerable --include-transitive` | PASS · no vulnerable packages reported for Core, Providers, App, Tests | run 39540874352 Windows job |
| 合成資料洩漏 regression | xUnit RPC error redaction, environment allowlist, Claude statusline field allowlist and CurrentUserOnly pipe test | PASS · 55/55 | run 39540874352; TRX artifact `windows-test-report` |
| Codex schema / protocol | Synthetic JSON-RPC and rate-limit response fixtures | PASS within 55/55 xUnit tests | run 39540874352 |
| Claude statusline parser / bridge | Official documented fields, synthetic JSON, PowerShell 5.1 parser, CurrentUserOnly pipe | PASS | run 39540874352 Windows xUnit and helper-parser steps; real Claude session remains unverified |
| Windows 11 interactive UAT | Issue #7 checklist | HUMAN_TEST_PENDING | Must be run on a Windows 11 x64 device; CI is not a substitute |

Monitor 不讀取 Codex 或 Claude credential 檔、不自動登入、不安裝或修復 CLI、不執行付費推論。Codex app-server 自行管理其帳號／連線狀態。Claude helper 只挑出官方 `five_hour` 與 `seven_day` rate-limit 欄位，透過 CurrentUserOnly pipe 傳入 Monitor；合成 session/transcript 欄位的洩漏測試通過。所有單元與 helper 整合 fixture 都是合成資料。

目前本機為受限 Linux sandbox，沒有 .NET SDK、Windows GUI 或 Windows PowerShell 5.1；未宣稱本機 Windows build、parser 或實機安全檢查通過。
