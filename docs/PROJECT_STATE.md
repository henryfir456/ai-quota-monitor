# PROJECT_STATE｜目前狀態

內容檢視日期：2026-10-21
工作目標：保留 v0.1.0 原始附件，修復 Issue #9 啟動崩潰並透過 Windows Release Gate 後建立 v0.1.1 prerelease。

此文件描述已定義的產品／架構範圍。當前 PR、commit、workflow run、Actions 配額、required checks 與 release 發布狀態以 GitHub 對應頁面為準，不在此複製易過期的結果。

## 本次交付進度

- v0.1.0 已發布後，使用者在 Windows 11 確認程式啟動崩潰。Issue #9 已記錄唯讀 `QuotaWindowViewModel.ProgressValue` 指向 WPF `ProgressBar.Value` 的預設 TwoWay Binding 為根因；本修正明確將相關 XAML Binding 設為 OneWay。
- PR #10 原先只有政策文字。本輪同 PR 補上 XAML regression、final-package GUI smoke、故意失敗反向案例、SHA256 成品同一性驗證與 fail-closed Release dependency。
- template PR #25 原先也只有政策文字；本輪同時補上可執行 helper、模板 CI contract/self-test 和採用端責任說明。
- PR head `9a3d6ddeeaacfc213d4523a1f502769397f0df09` 的 Windows `build-package-and-smoke` 與 Ubuntu governance 已在 [Actions run 39552400463](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39552400463) PASS：56/56 xUnit、最終 packaged EXE UI smoke、五秒穩定存活、零個匹配 crash events、XamlParseException 負向案例、EXE/ZIP SHA-256 身分驗證與 Gitleaks 均成功。Artifact ID `12323623137` 保存 sanitized evidence 和候選成品。
- v0.1.1 尚未發布：等待獨立 review、merge 與 merge 後 tag Release workflow 在完全相同上傳產物上完成 smoke/hash Gate；因此公開 Release 仍標記 `RELEASE_BLOCKED`。Windows Server 2025 Runner 不取代 Windows 11 實機 UAT。

- Issue #1～#7 的程式、設定、README／PRIVACY／SECURITY 與 portable release pipeline 已實作；PR #8 已開啟並關聯全部 Issues。
- PR run `39540874352` 在來源 SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f` 通過 Windows restore/build、55/55 xUnit、PowerShell helper parser/synthetic input、NuGet vulnerability audit；Ubuntu governance 32/32 與 Gitleaks 同樣通過。
- 此驗證也涵蓋 PowerShell JSON probe 的 terminating-error 修正。PR 審查、merge、tag 與 release 的即時狀態請以 PR #8 與 GitHub Actions 為準。
- Windows 11 x64 實機 UAT 為 `HUMAN_TEST_PENDING`；GitHub `windows-latest` runner 不能代替使用者實機驗收。Actions quota 為 `UNKNOWN`，目前未觀測到配額失敗。

## 實作範圍

- #1：.NET 8 WPF/MVVM solution、Core quota DTO、provider contract/coordinator、合成單元測試。
- #2：Codex 官方 app-server `account/rateLimits/read`、JSONL client、時間窗依 `windowDurationMins` 映射；不開啟 credential 檔。
- #3：Claude 官方 statusline JSON 欄位、使用者自行設定的本機 helper、CurrentUserOnly named pipe；不讀 credential 檔或修改 Claude 設定。
- #4：繁體中文 Claude/Codex 雙卡、5 小時與每週百分比、reset/countdown、失敗／過期／不支援狀態、深淺色主題與可選置頂精簡浮動面板。
- #5：系統匣、單一實例、300 秒預設輪詢、暫停／恢復自動監控、人工刷新節流、provider timeout/backoff、resume/network refresh、可選低額提醒與 Windows startup。
- #6：README、PRIVACY、SECURITY、threat model、synthetic leakage tests、NuGet audit、Gitleaks。
- #7：Windows x64 self-contained publish、portable ZIP、standalone EXE、SHA-256、SPDX SBOM、dependencies/test report 與 Windows 11 UAT 清單。

## 已核實邊界與待驗證事項

- Codex app-server 是實驗性協定；Codex Desktop/CLI target version 未取得，未執行 `--version`、登入或 app-server。
- Claude Code target version 與真實 session 欄位尚未知；未啟動 Claude CLI/session。
- 只有 Windows Actions 可提供 Windows build/test/publish 證據；GitHub Windows runner 不能替代 Windows 11 使用者實機 UAT。
- Windows 11 x64 實機 UAT 保持 `HUMAN_TEST_PENDING`，項目見 `docs/UAT_WINDOWS11.md`。
- 本次 repo main branch API snapshot：`protected=false`、required status checks 為 `off` 且空；repo rulesets API 為空，branch-protection 專用 API 回 403。重新合併前須再查當前狀態，403 不作為單獨結論。
- 開發環境為 Linux，沒有 .NET SDK、Windows GUI runtime 或 Windows PowerShell 5.1；本機 XML/YAML parse 不是 app compile/test。
- Windows `windows-latest` Runner 可驗證本次 Windows 版 UI 啟動，但不能替代使用者 Windows 11 裝置人工 UAT。
