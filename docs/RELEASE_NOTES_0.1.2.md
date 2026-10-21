# AI Quota Monitor v0.1.2（Windows x64 prerelease）

v0.1.2 修正 Claude Code 與 Codex 額度來源接線，並讓無法取得資料時的原因可直接辨識。

## 額度來源

- Claude Code 使用官方 statusline JSON 欄位 `rate_limits.five_hour` 與 `rate_limits.seven_day`。使用者可在 Claude 卡片確認一鍵設定；既有自訂 `statusLine` 會觸發衝突停止，完整原設定只在使用者確認後備份至本機，並可復原 Monitor 新增且未被修改的項目。
- Claude 官方 statusline 不保證所有方案都有額度欄位。未設定、未回報、設定後逾時、資料過期、解析失敗或不支援都會保留為明確狀態，不填入推測數字。
- Codex 使用官方 App Server `initialize`（宣告 `experimentalApi`）、`account/read`（`refreshToken=false`）和 `account/rateLimits/read`。`primary`／`secondary` 依 `windowDurationMins` 判斷視窗，不依順序猜測。
- Codex 會檢查常見 PATH、`CODEX_CLI_PATH`、npm/standalone CLI 位置與可列舉的 Windows Desktop package 位置；不開啟 credential 檔或取得明文 Token。
- 卡片與精簡面板顯示來源、最後成功更新時間、可理解的繁中狀態、固定錯誤代碼與下一步建議。

## 隱私及驗證範圍

- 所有自動協定 fixture 都是合成資料，不能作為真實帳號額度成功讀取的證明。
- 本版不自動登入、不讀取 Token、不安裝／修復 CLI、不觸發推論，也不傳送遙測。
- GitHub Windows Runner 的 packaged EXE smoke 只驗證桌面 UI 啟動與發布成品同一性；不等同 Windows 11 實機或 Claude/Codex 帳號額度驗證。
- 請先查看同一 Release 的 `WINDOWS_SMOKE_EVIDENCE.json`、`TEST_REPORT.txt` 和 `SHA256SUMS.txt`。Windows 11 UAT 與真實來源驗證仍依 `UAT_WINDOWS11.md` 標示。
