# 安全政策

## 支援版本

目前只支援本 Repository 的最新 `v0.1.x` prerelease。這是未簽章的 Windows x64 可攜程式；使用者應從 GitHub Release 下載並核對同一 Release 的 SHA-256。

## 回報漏洞

請勿在公開 Issue 附上 Token、帳號資料、真實 statusline JSON、日誌或含使用者名稱的路徑。請使用 GitHub 私下安全回報功能（若 Repo 已啟用）或聯絡 Repository 維護者，附最小可重現步驟與去識別化證據。尚未確認 private vulnerability reporting 設定前，請不要假設它已啟用。

## 安全邊界

- 不讀取 OAuth／憑證檔，不實作登入、refresh、修復、CLI 安裝、付費推論、遠端更新或遙測。
- Codex 只呼叫已文件化的唯讀 app-server `account/rateLimits/read`。子程序只收到必要 OS 路徑環境變數，stderr 丟棄、原始 RPC 錯誤不顯示，讀取後終止。
- Claude 僅由使用者手動配置的官方 statusline JSON 提供資料；named pipe 限目前 Windows 使用者，helper 只傳允許欄位。
- 所有 provider 輸入都視為不可信；無效／未知欄位不推測額度或時間。
- NuGet 套件版本固定，Windows restore 啟用 NuGet vulnerability audit；CI 另執行 Gitleaks。fixture 僅使用合成內容。

## 發行限制

v0.1.0 原 prerelease 保持不變；v0.1.1 修正版只會在最終 Windows packaged EXE 的 GUI startup smoke、故障反向控制和 SHA-256 同一性驗證全數通過後發布。版本未簽章，Windows 11 實機 UAT 項目仍可能標為 `HUMAN_TEST_PENDING`。自動測試、SHA-256、Linux 靜態檢查均不等同於 Windows 11 實機驗收、簽章或發行者身分驗證。
