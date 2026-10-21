# DEPLOYMENT｜交付與發行政策

## 專案設定

- 交付：Windows 11 x64 WPF self-contained portable ZIP 與 standalone EXE。
- 發行：GitHub `v0.1.0` prerelease，由 `.github/workflows/release.yml` Windows runner 建置與上傳。
- stable release、自動更新、安裝器、正式資料變更：不在本次範圍／未授權。
- 簽章：v0.1.0 unsigned；不宣稱 Defender／SmartScreen 認證。
- 回滾：可在 GitHub 將 prerelease 設為 draft/delete（需維護者執行）；不寫入使用者 CLI 設定或資料。

## 本次明確授權

本任務使用者明確要求獨立 feature branch、PR、符合政策後 squash merge，以及 GitHub prerelease。此授權只涵蓋 AI Quota Monitor Issues #1～#7 與 `v0.1.0` prerelease，不開啟 Auto-merge，也不授權 stable release、使用者資料修改、Secrets 或 CLI 安裝／修復。

## 必要自動 Gate

合併與發行前需確認：

- 同一 PR head SHA 的 Windows Actions `dotnet restore/build/test`、PowerShell 5.1 parser/bridge synthetic test、NuGet vulnerability audit、Gitleaks 結果成功。
- 同一 SHA 的獨立 reviewer、retrospective、PII_SCAN、README/PRIVACY/SECURITY 與 UAT pending 文件完成。
- 即時查核 `main` Branch Protection、inherited Rulesets、Required Checks 和 GitHub 權限；Required Checks 的 NOT CONFIGURED 不是 PASS，但明確確認未設定時不額外阻塞。
- GitHub repo mergeable/權限/所有適用內部 Gate 通過後，手動 Squash Merge；tag `v0.1.0` 只指向合併後的 main commit，再由 Windows Release workflow 建立 prerelease。

## 人工驗收時機

本專案的 UAT timing 已由本次使用者交付要求具體化：可在自動 Gate 全通過、merge 後建立 prerelease，並將 Windows 11 實機 UAT 留在 Release 附件／`docs/UAT_WINDOWS11.md` 的 `HUMAN_TEST_PENDING` 清單。這只適用於本次 prerelease；不能把 UAT pending 改寫為 PASS，也不能以 prerelease 作為 stable release 的驗收證據。

## CI 額度與備援

如果 Actions 回報明確 quota/runner 額度不足，不重跑相同工作。記錄 run URL、原因碼、head SHA、失敗範圍；使用可取得且同等範圍的 Windows 備援驗證，不能以 Linux 或不同 SHA 冒充 Windows CI/Required Check。若無同等 Windows runner，保留未完成交付與阻塞，繼續完成不依賴該 gate 的文件工作。

## 發行產物

Release workflow 由 Windows x64 runner 執行 restore/build/test、PowerShell helper 測試及 `dotnet publish -r win-x64 --self-contained true`，產生 portable ZIP、EXE、SHA256SUMS、SPDX SBOM、依賴清單、build metadata、test report、README、PRIVACY、SECURITY 和 UAT checklist。Release 附件須標 unsigned、來源 commit、CI runner 與 UAT pending；不得加入真實帳號、Token、quota 或可識別截圖。
