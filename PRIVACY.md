# 隱私說明

最後更新：2026-10-21。AI Quota Monitor v0.1.x 是本機 Windows 桌面程式，沒有自有後端、分析 SDK、廣告、遙測或自動更新。

## 處理的資料

- Codex：使用者按啟動／刷新後，程式以本機已安裝的 `codex.exe app-server` 執行官方 `account/read`（`refreshToken=false`）及 `account/rateLimits/read` RPC。前一個回應只用來判斷 `account` 是否為 null；不保留帳號欄位、Email 或原始回覆。程式不開啟 Codex 憑證檔、不讀出 Token、不記錄 app-server stderr。Codex app-server 可能使用它自行管理的登入狀態並按 Codex 的產品行為連線；本程式不取得或設定其 Token，也不選擇未經官方文件證實的 endpoint。
- Claude：Monitor 啟動時會讀取 `~/.claude/settings.json`，只用來辨識 `statusLine` 狀態與衝突，不顯示或記錄設定內容。只有使用者在 Claude 卡片確認設定時，Monitor 才會完整備份該檔、保留其他 JSON 欄位並加入自己的 statusline command。備份留在同一個 Claude 設定資料夾，可能含有使用者自行放入的私人設定值；不會傳出本機。helper 只保留 `rate_limits.five_hour` 和 `rate_limits.seven_day` 的百分比、reset 欄位，透過限本機目前使用者的 named pipe 傳給 Monitor；其他 session、prompt、transcript 和工作目錄欄位不會寫入檔案或送給 Monitor。
- UI 顯示必要的來源、觀測時間、百分比、reset 和資料狀態。Monitor 不保存原始帳號資料或 API 回覆。
- Monitor 偏好設定只保存在 `%LOCALAPPDATA%\AiQuotaMonitor\settings.json`，預期僅含主題、刷新間隔、視窗位置與浮動面板置頂偏好、低額度提醒選項及啟動偏好；不含憑證、Token、session ID 或額度回覆。Claude statusline helper 與設定交易記錄也留在該資料夾。監控暫停只在本次執行期間有效。若要重設或刪除偏好，請先結束 Monitor，再由 Windows 檔案總管刪除該設定檔；Claude 完整備份可另外在 `~/.claude` 檢視或刪除。

## 網路與分享

- Claude helper 不建立網路連線。Monitor 自身不發送遙測或帳號資料。
- Codex 額度查詢由 Codex app-server 依其既有產品實作執行；其底層網路路徑不是本程式定義的 API。Monitor 不另行連線 OpenAI／Anthropic 網站，也不把資料傳給專案維護者。
- GitHub Actions 僅在程式碼驗證時接收原始碼、合成測試 fixture 和測試結果；不執行使用者安裝的 Claude/Codex CLI，不含使用者 quota、帳號或 Token。
- Windows 發行 smoke mode 使用 runner 暫存資料夾的空白設定路徑，移除常見 Claude／Codex API key 環境變數，略過 Provider refresh／CLI 啟動；只輸出程式 PID、預期 UI 控制項數、視窗標題、例外型別及測試成品雜湊。測試不輸出例外訊息、帳號或額度欄位。

## 使用者控制

- Claude 預設不修改設定。設定前會讀取 `settings.json` 辨識現有 `statusLine`；使用者確認後才建立完整本機備份並新增 helper command。若有既存 statusline、格式錯誤或競爭修改，Monitor fail closed。復原只移除與記錄完全相符的 Monitor 項目，不覆寫使用者後來變更的內容；備份與 helper 檔不會自動刪除。
- 退出 Monitor 會停止輪詢並終止本程式啟動的 Codex app-server child process。Monitor 關閉後，Claude helper 不會保存或排隊狀態列資料。
- 可攜 ZIP 未簽章；SHA-256 只用於驗證下載檔案與 Release manifest 一致，不表示發行者身分已驗證。
