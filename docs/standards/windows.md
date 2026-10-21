# Windows CMD 與啟動器規範

- .bat 與 .cmd 必須以 CRLF checkout；依 Git .gitattributes 強制換行。
- 依實際目標 Windows CMD 的碼頁與字元集驗證包含中文的輸出，勿假設切換碼頁會修正腳本原始編碼。
- 測試引號、%~dp0、含空白與中文的路徑、參數轉送、錯誤碼、工具不存在的情境。
- 不可把 PowerShell 7 執行成功視為 Windows PowerShell 5.1 已通過。
