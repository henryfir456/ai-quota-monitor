# 測試報告

更新日期：2026-10-21。每個結果都須關聯完整 commit SHA、runner、命令與實際 workflow 證據。PR 前不可預填 PASS。

## 自動驗證

| 檢查 | 環境／命令 | 結果 | 證據 |
| --- | --- | --- | --- |
| 靜態專案／fixture 格式 | Linux · Python 3 XML/YAML/JSON parse | PASS | 11 XML、2 workflow YAML、6 JSON fixtures 可解析；不代表 C# compile |
| Repository config test | Linux · `node --test tests/agent-loop-config.test.cjs` | PASS | 1 suite passed；不涵蓋需要巢狀 Node process 的其他 suites |
| Repository governance tests | Ubuntu runner · `node --test tests/*.test.cjs` | BLOCKED locally; CI pending | Local command exited 1 because sandbox denied nested Node subprocesses with `EPERM`; dedicated CI job added |
| Restore | Windows runner · `dotnet restore AiQuotaMonitor.sln` | NOT RUN | 等 PR Actions |
| NuGet vulnerability audit | Restore 時 NU1901–NU1904 為錯誤；另列 `dotnet list AiQuotaMonitor.sln package --vulnerable --include-transitive` | NOT RUN | 等 PR Actions |
| Release build | Windows runner · `dotnet build AiQuotaMonitor.sln -c Release --no-restore` | NOT RUN | 等 PR Actions |
| Unit tests | Windows runner · `dotnet test AiQuotaMonitor.sln -c Release --no-build` | NOT RUN | TRX artifact 待產生 |
| Claude helper syntax／synthetic input | Windows PowerShell parser + synthetic stdin，驗證僅輸出兩視窗百分比 | NOT RUN | 等 PR Actions |
| Claude local bridge integration | Windows unit test launches only the bundled helper with synthetic JSON and verifies CurrentUserOnly pipe output | NOT RUN | 等 PR Actions |
| Gitleaks | Ubuntu GitHub Actions，full history | FAILED on initial Windows attempt; new Ubuntu run pending | Run 39535818420：Windows installer URL returned 404 before scan; moved to Ubuntu job |
| Windows x64 self-contained publish | Release tag workflow | NOT RUN | prerelease workflow 待執行 |

## 本機環境限制

開發 checkout 位於受限 Linux sandbox；`dotnet` 與 Windows GUI runtime 不存在。Node governance suite 的子程序測試也因 sandbox `EPERM` 未能完整執行。本地 restore/build/test、PowerShell 5.1 parser、WPF run、實際 Codex CLI、實際 Claude Code statusline 與 Windows 11 UAT 均未執行。Linux 治理測試或 GitHub Windows runner 結果不得寫成個人 Windows 11 實機通過。Release workflow 會在實際 Windows build 對應的來源 SHA 生成可下載 `TEST_REPORT.txt`，記錄 run URL、TRX 結果與 HUMAN_TEST_PENDING。

## 自動化測試涵蓋

- DTO 百分比界限、NaN／Infinity、null 值與 used/remaining 映射。
- 單一來源失敗隔離、每來源 timeout、呼叫者 cancellation。
- Codex 五小時／每週 duration mapping、weekly-only、0／100%、缺少／毫秒 reset、錯誤百分比、未知 duration、錯誤 bucket、重複視窗和格式錯誤。
- Codex JSONL notification／response id correlation、RPC 錯誤訊息不外洩、CLI 未安裝／RPC 不支援／失敗狀態、子程序環境變數 allowlist。
- Claude 官方 statusline 欄位、百分比/reset 驗證、缺值狀態立即清除前一筆數值與 15 分鐘過期處理；Windows bridge integration 也驗證缺額度時送出 UNKNOWN/N/A 快照。
- 設定 JSON atomic save、壞檔／未知 schema fallback、敏感欄位不存在及有效值範圍。

測試不接觸真實 Token、帳號、供應商網路或付費推論。
