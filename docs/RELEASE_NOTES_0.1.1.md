# AI Quota Monitor v0.1.1（Windows x64 prerelease）

此修正版處理 v0.1.0 的 Windows 啟動崩潰：WPF `ProgressBar.Value` 對唯讀 `ProgressValue` 使用預設 TwoWay Binding，造成啟動時 `XamlParseException`。Binding 現在明確使用 `OneWay`，並有回歸測試。

Release workflow 會在 Windows GitHub Actions Runner 先 build、package，再啟動最終 portable EXE；檢查視窗與 UI 已初始化、程序在有界穩定期間正常存活、沒有未處理例外，並用故意啟動失敗的反向案例驗證 Gate。通過後核對 smoke-tested EXE／ZIP SHA-256，才建立 prerelease。啟動驗證使用離線安全模式，不讀取 Token、帳號或 quota，也不執行 Claude／Codex Provider。

v0.1.0 的 tag 和附件保持原樣。v0.1.1 未程式碼簽章。Windows 11 實機 UAT 仍是 `HUMAN_TEST_PENDING`；GitHub Windows Server Runner 證據不等於 Windows 11 使用者實機驗收。
