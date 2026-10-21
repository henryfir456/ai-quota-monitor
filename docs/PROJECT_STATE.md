# PROJECT_STATE｜目前狀態

內容檢視日期：2026-10-21
工作目標：交付 GitHub Issues #1～#7 的 Windows 11 x64 AI Quota Monitor v0.1.0 prerelease。

此文件描述已定義的產品／架構範圍。當前 PR、commit、workflow run、Actions 配額、required checks 與 release 發布狀態以 GitHub 對應頁面為準，不在此複製易過期的結果。

## 實作範圍

- #1：.NET 8 WPF/MVVM solution、Core quota DTO、provider contract/coordinator、合成單元測試。
- #2：Codex 官方 app-server `account/rateLimits/read`、JSONL client、時間窗依 `windowDurationMins` 映射；不開啟 credential 檔。
- #3：Claude 官方 statusline JSON 欄位、使用者自行設定的本機 helper、CurrentUserOnly named pipe；不讀 credential 檔或修改 Claude 設定。
- #4：繁體中文 Claude/Codex 雙卡、5 小時與每週百分比、reset/countdown、失敗／過期／不支援狀態、深淺色主題與精簡浮動面板。
- #5：系統匣、單一實例、300 秒預設輪詢、人工刷新節流、provider timeout/backoff、resume/network refresh、可選低額提醒與 Windows startup。
- #6：README、PRIVACY、SECURITY、threat model、synthetic leakage tests、NuGet audit、Gitleaks。
- #7：Windows x64 self-contained publish、portable ZIP、standalone EXE、SHA-256、SPDX SBOM、dependencies/test report 與 Windows 11 UAT 清單。

## 已核實邊界與待驗證事項

- Codex app-server 是實驗性協定；Codex Desktop/CLI target version 未取得，未執行 `--version`、登入或 app-server。
- Claude Code target version 與真實 session 欄位尚未知；未啟動 Claude CLI/session。
- 只有 Windows Actions 可提供 Windows build/test/publish 證據；GitHub Windows runner 不能替代 Windows 11 使用者實機 UAT。
- Windows 11 x64 實機 UAT 保持 `HUMAN_TEST_PENDING`，項目見 `docs/UAT_WINDOWS11.md`。
- 本次 repo main branch API snapshot：`protected=false`、required status checks 為 `off` 且空；repo rulesets API 為空，branch-protection 專用 API 回 403。重新合併前須再查當前狀態，403 不作為單獨結論。
- 開發環境為 Linux，沒有 .NET SDK、Windows GUI runtime 或 Windows PowerShell 5.1；本機 XML/YAML parse 不是 app compile/test。
