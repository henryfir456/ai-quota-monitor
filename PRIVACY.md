# 隱私說明

最後更新：2026-10-21。AI Quota Monitor v0.1.0 是本機 Windows 桌面程式，沒有自有後端、分析 SDK、廣告、遙測或自動更新。

## 處理的資料

- Codex：使用者按啟動／刷新後，程式以本機已安裝的 `codex.exe app-server` 執行官方 `account/rateLimits/read` RPC，解析回傳的 5 小時／每週百分比和 reset 時間。程式不開啟 Codex 憑證檔、不讀出 Token、不記錄原始回覆或 app-server stderr。Codex app-server 可能使用它自行管理的登入狀態並按 Codex 的產品行為連線；本程式不取得或設定其 Token，也不選擇未經官方文件證實的 endpoint。
- Claude：只有使用者自行選擇設定官方 Claude Code statusline helper 後，Claude 傳給 statusline 的本機 JSON 才會被 helper 處理。helper 只保留 `rate_limits.five_hour` 和 `rate_limits.seven_day` 的百分比、reset 欄位，透過限本機目前使用者的 named pipe 傳給 Monitor；其他 session、prompt、transcript 和工作目錄欄位不會寫入檔案或送給 Monitor。
- UI 顯示必要的來源、觀測時間、百分比、reset 和資料狀態。Monitor 不保存原始帳號資料或 API 回覆。
- 使用者設定只保存在 `%LOCALAPPDATA%\AiQuotaMonitor\settings.json`，預期僅含主題、刷新間隔、視窗偏好、低額度提醒選項及啟動偏好；不含憑證、Token、session ID 或額度回覆。使用者可在程式設定中重設／刪除這些偏好。

## 網路與分享

- Claude helper 不建立網路連線。Monitor 自身不發送遙測或帳號資料。
- Codex 額度查詢由 Codex app-server 依其既有產品實作執行；其底層網路路徑不是本程式定義的 API。Monitor 不另行連線 OpenAI／Anthropic 網站，也不把資料傳給專案維護者。
- GitHub Actions 僅在程式碼驗證時接收原始碼、合成測試 fixture 和測試結果；不執行使用者安裝的 Claude/Codex CLI，不含使用者 quota、帳號或 Token。

## 使用者控制

- Claude 來源預設未配置。使用者必須自行把隨附 statusline command 加到 Claude Code 設定；Monitor 不讀取、修改或合併 `settings.json`。若已有 statusline，使用者需自行決定如何整合，避免覆蓋自己的指令。
- 退出 Monitor 會停止輪詢並終止本程式啟動的 Codex app-server child process。Monitor 關閉後，Claude helper 不會保存或排隊狀態列資料。
- 可攜 ZIP 未簽章；SHA-256 只用於驗證下載檔案與 Release manifest 一致，不表示發行者身分已驗證。
