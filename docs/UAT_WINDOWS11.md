# Windows 11 x64 人工 UAT 清單

目前狀態：`HUMAN_TEST_PENDING`。本文件是待執行項目，不代表已通過。請在非系統管理員的 Windows 11 x64 使用者工作階段，用本次 Release 的 portable ZIP，記錄 Release tag、完整 commit SHA、Windows build、CPU 架構、測試者代號與每列 PASS／FAIL／NOT RUN。不要附真實 quota、帳號識別、Token、session 資料或未遮蔽截圖。

| # | 驗收項目 | 預期結果 | 狀態／證據 |
| --- | --- | --- | --- |
| 1 | 解壓 portable ZIP 並雙擊 `AiQuotaMonitor.exe` | 不需安裝／管理員權限；啟動無 console；只存在一個 Monitor process | NOT RUN |
| 2 | Codex 已安裝且可用 | 額度由官方 app-server 顯示；五小時與 weekly duration／剩餘百分比／reset 符合來源；無 login/inference prompt | NOT RUN · 記錄 Codex 版本但不附帳號資料 |
| 3 | Codex CLI 不存在／帳號不可用／method 不支援 | UNKNOWN、ERROR 或 UNSUPPORTED 清楚顯示；不安裝、不登入、不修復、不顯示假數字 | NOT RUN |
| 4 | Claude 未設定 statusline | 兩視窗顯示 UNKNOWN/N/A，不出現預設數字 | NOT RUN |
| 5 | 使用者手動設定 Claude helper 且官方欄位可用 | UI 顯示 5 小時／7 日額度及 reset；session id／transcript path 不進 Monitor；原 statusline 依使用者配置保留或整合 | NOT RUN · 僅合成資料可用於 helper smoke test |
| 6 | 0%、100%、小數百分比、缺 reset、錯誤／缺欄位、超過 15 分鐘舊 Claude 資料 | 只呈現來源真實值；缺值 N/A；舊資料 stale；不補估 | NOT RUN |
| 7 | 低額度提醒關閉／開啟 20% 與 10% 門檻 | 預設關閉；最新實際來源低於門檻才提醒；每供應商／視窗／門檻／reset window 去重；unknown/stale/error 不提醒 | NOT RUN · 請避免把真實數值寫入公開報告 |
| 8 | 主題、已用／剩餘切換、卡片收合、立即刷新 | 控制操作有效；手動刷新 10 秒節流；百分比模式與 bar 一致 | NOT RUN |
| 9 | 深色／淺色與文字縮放／鍵盤操作 | 對比與焦點可見；常用按鈕可鍵盤操作；Automation name 可讀 | NOT RUN |
| 10 | 系統匣顯示／隱藏、關閉主視窗、再次開啟、第二次啟動 | 選單可切換主視窗顯示／隱藏；關閉視窗縮至系統匣；雙擊恢復；第二次啟動聚焦第一個視窗 | NOT RUN |
| 11 | 精簡浮動面板 | 可拖曳、縮放；設定中可切換置頂；跨螢幕／DPI 移動後重開位置可恢復；位置超出顯示器時回到可見工作區 | NOT RUN |
| 12 | 睡眠／恢復、網路失聯／恢復、provider timeout | 喚醒／恢復網路後刷新；無重疊；來源間隔離；退避不超過 30 分鐘 | NOT RUN |
| 13 | 設定 interval、低額度提醒、登入啟動 | 60～1800 秒；HKCU startup 預設關閉且只在使用者選取後設定；錯誤設定被拒絕 | NOT RUN |
| 14 | 系統匣結束、工作管理員與 process tree | 主程式退出；Monitor 啟動的 Codex app-server 不殘留；不刪改 CLI 設定／憑證 | NOT RUN |
| 15 | Release SHA-256、portable ZIP 內容、無簽章告知 | `SHA256SUMS.txt` 與下載檔一致；ZIP 含 README／PRIVACY／SECURITY／SBOM／helper | NOT RUN |
| 16 | 系統匣／主視窗暫停與恢復監控 | 暫停停止週期及網路／喚醒自動刷新；進行中的刷新完成後不續排；手動刷新仍可用；恢復時立即刷新 | NOT RUN |
| 17 | 安裝 Issue #9 修正後的 v0.1.1 prerelease 並雙擊啟動 | 可見主視窗保持開啟；Task Manager 有 `AiQuotaMonitor`；此次啟動沒有 .NET Runtime 1026；關閉主視窗後程式依既有設計常駐系統匣 | NOT RUN · 僅在該 prerelease 通過 Windows Actions smoke gate 後執行；不附事件原文或個人路徑 |

UAT 結果只能在實際 Windows 11 裝置完成後更新。CI 建置、單元測試、Release asset 下載或 Linux 執行都不能代替第 1～16 項實機結果。
