# Issue #9 / PR #10 / Template PR #25 實作計畫

## 範圍與成功條件

- 修正 `ProgressValue` 對 `ProgressBar.Value` 的唯讀 TwoWay binding，並以 STA/WPF 回歸測試建立實際主視窗、資料卡與四個進度條。
- 為最終 Windows x64 portable EXE 加入不連線、不呼叫 Provider、不讀使用者設定的 smoke mode；驗證視窗可見、UI 初始化、程序穩定存活、沒有未處理例外。
- 加入故意啟動失敗的反向案例；Gate 必須辨識它並令 Release job 失敗。
- Release 僅接受 `v0.1.1`，嚴格 build → package → smoke/reverse test → SHA-256/同一成品驗證 → Release；通過後不得重建或重封裝。
- 在模板 PR #25 補上可執行的共用 smoke helper、Windows 負反向測試和 CI contract，並在文件說清模板提供 helper、採用端必須接到實際 Release workflow。
- 更新 README、AGENTS、部署／測試狀態、回顧；由獨立 Reviewer 檢查最終 head SHA；只有 Windows Actions Gate 和 repo policy 都通過才合併／發布。

## 依序任務

1. 加入會失敗的 WPF Binding 回歸測試；執行 Windows CI 紅燈證據。
2. 修正 XAML，加入安全 smoke mode、UI ready marker、錯誤標記和人工注入 startup failure。
3. 實作 PowerShell Windows GUI smoke/reverse gate 和 sanitized evidence。
4. 重排 Windows CI 與 Release workflow，確保最終封裝檔案同一性、失敗即阻止後續、無 `always()`／`continue-on-error`。
5. 更新 #10 repo 的 docs、tests、AGENTS、release metadata 和 retrospective。
6. 在 template PR #25 加入可重用 gate helper、Windows self-test、CI 合約與責任文件。
7. 對兩個 repo 執行本機可得驗證、GitHub Windows / governance CI、PII scan；獨立 review 並修正發現。
8. 查核當前 Required Checks、Review、workflow 結果與授權；符合時 squash merge。#10 merge 後 tag `v0.1.1`，只讀取 release Actions 結果；未通過則 `RELEASE_BLOCKED`。

## 不變條件

- 不讀取、記錄或傳送 OAuth Token； smoke mode 不啟動 Claude/Codex Provider、不存取帳號或額度來源。
- 不執行 v0.1.0 EXE；保留 v0.1.0 成品；不以 Linux 編譯或靜態檢查宣稱 Windows GUI PASS。
- smoke 與 Release evidence 只含 runner、window/UI 訊號、例外類型、SHA 和 workflow URL，不含 log 訊息、帳號或 quota 值。
- Actions 配額／Runner 不可用時不重跑同因工作、不發布未驗證 EXE，保留阻塞證據與可執行解阻步驟。
