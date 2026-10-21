# PowerShell 工程規範

- 首先辨識目標是 Windows PowerShell 5.1 還是 PowerShell 7+；不得以 pwsh 7 取代 5.1 的相容性證據。
- 需在 Windows PowerShell 5.1 解析的 .ps1/.psm1/.psd1，使用 UTF-8 BOM，驗證前 3 位元組 EF BB BF。PowerShell 7 專用專案可按 REPO_FACTS 定義其他編碼，不能覆蓋舊版相容性要求。
- 採嚴謹錯誤處理、引號與引數陣列；測試中文字、空白路徑與 exit code。
- 先跑 Parser 檢查，再以 Mock 測試行為；需連硬體時，另留真機驗收證據。
- 不能把顯示碼頁 chcp 65001 當作 PowerShell 5.1 的無 BOM 腳本解碼修正。
