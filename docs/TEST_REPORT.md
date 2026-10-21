# 測試報告

更新日期：2026-10-21。每個結果只適用於表列的完整 commit SHA、runner、命令與 workflow 證據。後續 head 以 PR #8 Checks 為準；Release runner 會將最終 SHA、run URL、TRX 與 UAT 狀態寫入下載的 `TEST_REPORT.txt`。

## 自動驗證

| 檢查 | 環境／命令 | 結果 | 證據 |
| --- | --- | --- | --- |
| 靜態專案／fixture 格式 | Linux · Python 3 XML/YAML/JSON parse | PASS | 12 XML、2 workflow YAML、6 test JSON 可解析；不代表 C# compile |
| Repository config test | Linux · `node --test tests/agent-loop-config.test.cjs` | PASS | 1 suite passed；不涵蓋需要巢狀 Node process 的其他 suites |
| Repository governance tests | Ubuntu runner · `node --test tests/*.test.cjs` | PASS on SHA `00ddaa249334a780d9d42f98c7d62d22574f7af0`; local sandbox BLOCKED | PR run 39537932515 governance job passed. Local command exits 1 because sandbox denies nested Node subprocesses with `EPERM` |
| Gitleaks | Ubuntu GitHub Actions，full history | PASS on SHA `00ddaa249334a780d9d42f98c7d62d22574f7af0` | PR run 39537932515 governance job passed; earlier Windows installer 404 was avoided by moving scan to Ubuntu |
| Restore | Windows runner · `dotnet restore AiQuotaMonitor.sln` | PASS on SHA `00ddaa249334a780d9d42f98c7d62d22574f7af0` | PR run 39537932515 `build-and-test` job |
| Release build | Windows runner · `dotnet build AiQuotaMonitor.sln -c Release --no-restore` | FAIL on SHA `00ddaa249334a780d9d42f98c7d62d22574f7af0`; corrected in next commit, final head pending | PR run 39537932515 failed because tests lacked explicit `System.IO` global using; previous SHA `7645556...` had a separate CS9007 raw-string failure, also fixed |
| Unit tests / PowerShell helper / NuGet vulnerability audit | Windows runner · `dotnet test`, `Test-ClaudeStatusline.ps1`, `dotnet list ... --vulnerable --include-transitive` | SKIPPED on SHA `00ddaa249334a780d9d42f98c7d62d22574f7af0`; final head pending | Build failure prevented later steps; no test or audit result is inferred |
| Windows x64 self-contained publish | Release tag workflow | NOT RUN | prerelease workflow 待執行 |

## 本機環境限制

開發 checkout 位於受限 Linux sandbox；`dotnet` 與 Windows GUI runtime 不存在。Node governance suite 的子程序測試因 sandbox `EPERM` 未能完整執行。本地 restore/build/test、PowerShell 5.1 parser、WPF run、實際 Codex CLI、實際 Claude Code statusline 與 Windows 11 UAT 均未執行。Linux 治理測試或 GitHub Windows runner 結果不得寫成個人 Windows 11 實機通過。Release workflow 會在實際 Windows build 對應的來源 SHA 生成可下載 `TEST_REPORT.txt`，記錄 run URL、TRX 結果與 HUMAN_TEST_PENDING。

## 自動化測試涵蓋

- DTO 百分比界限、NaN／Infinity、null 值與 used/remaining 映射。
- 單一來源失敗隔離、每來源 timeout、呼叫者 cancellation。
- Codex 五小時／每週 duration mapping、weekly-only、0／100%、缺少／毫秒 reset、錯誤百分比、未知 duration、錯誤 bucket、重複視窗和格式錯誤。
- Codex JSONL notification／response id correlation、RPC 錯誤訊息不外洩、CLI 未安裝／RPC 不支援／失敗狀態、子程序環境變數 allowlist。
- Codex child process startup reads only explicit OS/profile environment variable names and does not enumerate the parent process environment.
- Claude 官方 statusline 欄位、百分比/reset 驗證、缺值狀態立即清除前一筆數值與 15 分鐘過期處理；Windows bridge integration 也驗證缺額度時送出 UNKNOWN/N/A 快照。
- 設定 JSON atomic save、壞檔／未知 schema fallback、敏感欄位不存在、有效值範圍與浮動面板置頂偏好。
- 監控暫停政策：停止排程與 lifecycle refresh、禁止低額提醒、仍允許手動刷新；對應 tray／resume 實機項目在 UAT #16。

測試不接觸真實 Token、帳號、供應商網路或付費推論。
