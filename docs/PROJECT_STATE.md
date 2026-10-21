# PROJECT_STATE｜目前狀態

內容檢視日期：2026-10-21
工作目標：保留 v0.1.0 與 v0.1.1 原始附件，完成 Issue #11 的 Claude/Codex 可驗證額度來源及繁中診斷，並在 Windows Release Gate 通過後建立 v0.1.2 prerelease。

此文件描述已定義的產品／架構範圍。當前 PR、commit、workflow run、Actions 配額、required checks 與 release 發布狀態以 GitHub 對應頁面為準，不在此複製易過期的結果。

## 本次交付進度

- Issue #11 分支 `codex/issue-11-quota-sources` 已完成 v0.1.2 候選實作：Codex initialize 加入 `experimentalApi=true`，以 `account/read`（`refreshToken=false`）辨識未登入，再讀官方額度 RPC；Claude statusline 增加使用者確認、完整本機備份、衝突保護與可復原設定流程。
- Claude/Codex 卡片與浮動面板呈現來源、最後成功時間、細分狀態、固定錯誤代碼及繁中建議。合成測試覆蓋成功、未設定／未安裝、未登入、解析失敗、逾時和過期；這些資料不是實際帳號額度。
- PR head `349e78300d55fccafe1d97d467197364803291a0` 的 Windows Server 2025 packaged-EXE gate 與 Ubuntu governance 已於 [Actions run 39576164781](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39576164781) PASS；Windows xUnit 123/123、PowerShell helper/parser、dependency audit、最終 EXE 正向啟動／故意失敗反向測試及同一產物 SHA 驗證通過。EXE SHA-256 `f20366fbc387331b21682e4552d2dc43c462f1e2d589870d43e2cb931192102f`、portable ZIP SHA-256 `65a86f0c73378d8b247e4f57b1f23ff180ea4e0be4f770c4eb311b4a5cd10cc0`；artifact ID `12333918616`。獨立 review 隨後發現 publisher 未把 SHA256SUMS.txt 附加至 Release；最新修正將公開 checksum 清單納入預期附件，並以 draft metadata 驗證其獨立 SHA，還加入 incomplete `starter` 附件的安全復原。這些最新發布修正仍需 exact-head CI 與 review。Windows 11 實機與真實供應商帳號仍待驗證。
- PR #12 的 merge、Release 尚未完成。Windows Runner 已具備本輪 C#／WPF 與最終 packaged EXE 測試證據；Linux 開發環境本身沒有 `dotnet`、`pwsh` 或 GUI，不宣稱本機 Windows 測試。
- Codex Desktop 實際 package 位置/ACL、已安裝 CLI 版本，以及真實 Claude/Codex 帳號是否提供額度欄位均 `NOT VERIFIED`。Windows 11 真實帳號 UAT 保持 `HUMAN_TEST_PENDING`。
- Release 建置使用 v0.1.2，入口限定為 `main` 上固定 `v0.1.2` 的手動 `workflow_dispatch`；最終 publisher 先建立 draft，使用已通過 Windows gate 的同一組附件，比對 manifest 指紋與遠端下載 SHA-256，包含 manifest 本身，再完整公開。中斷只允許同 SHA、同 manifest draft 續跑；不完整 `starter` 附件會在該 draft 內刪除後重傳。不同來源／附件、已發布 Release 或未知狀態均 fail closed。

- v0.1.0 已發布後，使用者在 Windows 11 確認程式啟動崩潰。Issue #9 已記錄唯讀 `QuotaWindowViewModel.ProgressValue` 指向 WPF `ProgressBar.Value` 的預設 TwoWay Binding 為根因；本修正明確將相關 XAML Binding 設為 OneWay。
- PR #10 原先只有政策文字。本輪同 PR 補上 XAML regression、final-package GUI smoke、故意失敗反向案例、SHA256 成品同一性驗證與 fail-closed Release dependency。
- template PR #25 原先也只有政策文字；本輪同時補上可執行 helper、模板 CI contract/self-test 和採用端責任說明。
- Template PR #25 已補強 `publish.yml`／`ncipollo/release-action` Release Gate contract 反例；其當前 head run [39554142920](https://github.com/henryfir456/agent-project-template/actions/runs/39554142920) 兩個 jobs 均沒有 runner steps/logs，因此 helper parser/harness 未執行，原因 `UNKNOWN`，不重跑舊環境失敗。詳 PR #25；不把它標為 CI PASS。
- 修正程式來源 SHA `61b64cef0f58ec16064e3213698e89e89a5b63d9` 的 Windows `build-package-and-smoke` 與 Ubuntu governance 已在 [Actions run 39554220174](https://github.com/henryfir456/ai-quota-monitor/actions/runs/39554220174) PASS：56/56 xUnit、最終 packaged EXE UI smoke、五秒穩定存活、零個匹配 crash events、XamlParseException 負向案例、EXE/ZIP SHA-256 身分驗證與 Gitleaks 均成功。Artifact ID `12324264835` 保存 sanitized evidence 與候選成品；EXE SHA `ccce850276056beaeecd0479ec28633bd741ca64b5faaebb615f94be7eeb425e`、ZIP SHA `a7805935bf50c108612c18ca8b4247610298a7336711c03833b53bf8b709a851`。此為測試報告提交前的程式碼候選來源；後續 PR head 狀態以其 Actions Checks 為準。
- v0.1.1 prerelease 已由上一輪工作發布並由使用者確認在 Windows 11 成功啟動；該結果只證明 v0.1.1 GUI 啟動，不代表 Claude/Codex 額度讀取成功。本輪不覆寫既有 v0.1.1 附件。

- Issue #1～#7 的程式、設定、README／PRIVACY／SECURITY 與 portable release pipeline 已實作；PR #8 已開啟並關聯全部 Issues。
- PR run `39540874352` 在來源 SHA `24e31940caa191214cc3896f5c0a6d0b2f686f0f` 通過 Windows restore/build、55/55 xUnit、PowerShell helper parser/synthetic input、NuGet vulnerability audit；Ubuntu governance 32/32 與 Gitleaks 同樣通過。
- 此驗證也涵蓋 PowerShell JSON probe 的 terminating-error 修正。PR 審查、merge、tag 與 release 的即時狀態請以 PR #8 與 GitHub Actions 為準。
- Windows 11 x64 實機 UAT 為 `HUMAN_TEST_PENDING`；GitHub `windows-latest` runner 不能代替使用者實機驗收。Actions quota 為 `UNKNOWN`，目前未觀測到配額失敗。

## 實作範圍

- #1：.NET 8 WPF/MVVM solution、Core quota DTO、provider contract/coordinator、合成單元測試。
- #2：Codex 官方 app-server capability handshake、唯讀 `account/read`（不 refresh Token）、`account/rateLimits/read`、JSONL client、時間窗依 `windowDurationMins` 映射；支援多種 Windows 安裝路徑，不開啟 credential 檔。
- #3：Claude 官方 statusline JSON 欄位、本機 helper 與 CurrentUserOnly named pipe；使用者確認後才備份／更新設定，衝突保留原設定並提供復原；不讀 credential 檔。
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
