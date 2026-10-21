# DEPLOYMENT｜交付與發行政策

## 專案設定

- 交付：Windows 11 x64 WPF self-contained portable ZIP 與 standalone EXE。
- 原版：GitHub `v0.1.0` prerelease，保留原 tag 與附件，不覆寫。
- 修正版目標：GitHub `v0.1.2` prerelease；`.github/workflows/release.yml` 僅接受 `main` 手動 dispatch。所有 Windows packaged EXE、反向案例及 SHA-256 Gate 通過後，最後的 publisher 才以 GitHub API 原子建立指向已測 `github.sha` 的 tag，再以 `--verify-tag` 建立 Release。
- stable release、自動更新、安裝器、正式資料變更：不在本次範圍／未授權。
- 簽章：v0.1.0／v0.1.1／v0.1.2 unsigned；不宣稱發行者簽章、真實 Claude/Codex 帳號驗證或 Windows 11 實機 UAT。
- 回滾：可在 GitHub 將 prerelease 設為 draft/delete（需維護者執行）；不寫入使用者 CLI 設定或資料。

## 本次明確授權

使用者本次明確授權完成 Issue #11 對應修正、建立 PR、獨立 review，並於所有適用 Gate 通過後 Squash Merge 至 `main`；Windows Release Gate 通過後，以受限 `workflow_dispatch` 發布 `v0.1.2` prerelease。此為一次性授權，不開啟 Auto-merge，也不授權 stable release、讀取 Token／credential files、安裝／修復 CLI、登入或執行付費推論。Claude `settings.json` 只在使用者於本機明確確認設定後才備份與修改。

## 必要自動 Gate

合併與發行前需確認：

- 同一 PR head SHA 的 Ubuntu governance/Gitleaks、Windows Actions `dotnet restore/build/test`、PowerShell parser/bridge synthetic test、NuGet vulnerability audit 與最終 portable package GUI smoke／反向測試結果成功。
- 同一 SHA 的獨立 reviewer、retrospective、PII_SCAN、README/PRIVACY/SECURITY 與 UAT pending 文件完成。
- 即時查核 `main` Branch Protection、inherited Rulesets、Required Checks 和 GitHub 權限；Required Checks 的 NOT CONFIGURED 不是 PASS，但明確確認未設定時不額外阻塞。
- Release workflow 嚴格依序 build → package → Windows smoke test → SHA-256 驗證 → Release。Smoke 必須啟動打包 EXE，檢查可見視窗／應用 UI、readiness JSON、程序穩定存活、Windows Application Event 1000／.NET Runtime Event 1026 與故障反向控制；同一 EXE 和 ZIP 沿用到 SHA 驗證及 Release，不可重新封裝。任何 FAIL／UNKNOWN／cancel／timeout／無證據都阻止後續 Release。
- GitHub repo mergeability、權限、Required Checks 與所有 Gate 通過後手動 Squash Merge PR。Release dispatch 只能從 `main` 執行；跨 ref 共用固定 concurrency group，tag 固定為 `v0.1.2` 且既有 tag 一律拒絕。所有 Gate 通過後，publisher 以 GitHub API 建立指向 `github.sha` 的 tag，讀回並核對同一 SHA，再用 `--verify-tag` 建立 Release。v0.1.0 與 v0.1.1 成品不修改、不覆寫。

## 人工驗收時機

Windows 11 實機 UAT 可留在 `docs/UAT_WINDOWS11.md` 的 `HUMAN_TEST_PENDING` 清單；GitHub Windows Server runner 只能驗證 Windows 版實際 GUI 啟動，不能代替使用者 Windows 11 裝置測試，也不可將 pending 改寫為 PASS。

## CI 額度與備援

如果 Actions 回報明確 quota/runner 額度不足，不重跑相同工作。記錄 run URL、原因碼、head SHA、失敗範圍；使用可取得且同等範圍的 Windows 備援驗證，不能以 Linux 或不同 SHA 冒充 Windows CI/Required Check。若無同等 Windows runner，保留未完成交付與阻塞，繼續完成不依賴該 gate 的文件工作。

## 發行產物

Release workflow 由 Windows x64 runner 執行 restore/build/test、PowerShell helper、`dotnet publish -r win-x64 --self-contained true`，產生 portable ZIP／EXE 後啟動最終 standalone EXE。Smoke 使用隔離設定路徑、移除 Provider API key 環境變數且不呼叫任何 Provider／CLI；ready 訊號包含視窗與卡片／進度條 Binding 狀態，不含帳號、Token 或額度。通過後建立 SHA256SUMS、`TEST_REPORT.txt` 與 `WINDOWS_SMOKE_EVIDENCE.json`；Release job 再次驗證兩個主要產物的雜湊及 PASS evidence。附件標 unsigned、來源 commit、Actions URL、Runner 和 UAT pending；不附真實帳號、Token、quota 或含敏感內容的原始 log。
