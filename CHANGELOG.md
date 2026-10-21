# 更新紀錄

遵循 Keep a Changelog 格式。Release tag、app version 與此文件版本保持一致。

## [0.1.2] - 2026-10-21

### 新增
- Claude 卡片提供 statusline 一鍵設定與復原；設定前檢查衝突、使用者確認後建立完整本機備份，復原只移除未變更的 Monitor 設定。
- Claude、Codex 顯示資料來源、最近成功更新時間、繁中狀態、固定安全錯誤碼與處理建議。
- Codex App Server 初始化宣告 `experimentalApi`，使用 `account/read` 的 `refreshToken=false` 區分未登入，再讀取 `account/rateLimits/read`。
- Claude CLI 定位涵蓋官方 Windows native `%USERPROFILE%\.local\bin\claude.exe`、npm 與 PATH；Codex 定位涵蓋 PATH、`CODEX_CLI_PATH`、`CODEX_HOME` standalone/app-server、npm 原生 binary 及可列舉的 Windows Desktop 套件位置。

### 修正
- Claude helper 將 JSON 解析失敗以固定錯誤碼送入 CurrentUserOnly pipe，避免失敗被誤報為成功或單純欄位缺席。
- 設定後未收到 statusline 的 15 分鐘等待會明確逾時；過期成功資料仍標示過期並保留最後成功時間。
- 額度讀取失敗時不留舊百分比冒充最新資料，診斷更新會即時反映於主視窗與浮動面板。

### 安全
- 不讀取或記錄 OAuth Token、Codex/Claude 憑證檔，不登入、refresh、安裝／修復 CLI 或執行推論。
- Codex `account/read` 只判斷 `account` 是否為 null，不保存或顯示帳號欄位；Claude helper 僅傳官方 rate-limit 欄位或固定錯誤碼。
- 增加已登入／未登入、成功、未設定／未安裝、解析失敗、逾時與過期的合成協定測試；真實供應商帳號驗證需 Windows 11 UAT。

### 發行
- Windows x64 Release workflow 僅接受 main 上固定 `v0.1.2` 的 `workflow_dispatch`，依 build → package → smoke test → SHA-256 驗證 → Release 順序執行並沿用同一成品；中斷後只有來源 SHA 相同且尚無 Release 的既有 tag 可續跑。

## [0.1.1] - 2026-10-21

### 修正
- 修正 WPF `ProgressBar.Value` 對唯讀 `QuotaWindowViewModel.ProgressValue` 的預設 TwoWay binding，所有進度顯示／條件 Binding 明確使用 `OneWay`。
- 新增無 Provider、帳號、Token 讀取的 Windows UI startup smoke mode；Release Gate 會驗證最終 portable EXE、視窗與 UI ready 訊號、程序穩定性、例外與相同 SHA-256，並執行故意失敗的反向案例。
- v0.1.0 成品與 tag 保留不變；v0.1.1 只在 Windows Gate 實際通過後發布。

## [0.1.0] - 2026-10-21

Windows x64 prerelease 候選。包含 Codex 官方 app-server readonly provider、Claude Code 官方 statusline bridge（使用者選擇性配置）、繁體中文 WPF 卡片、系統匣、浮動面板、深／淺主題、使用者提醒設定及 self-contained portable 發行流程。Windows 11 實機 UAT 狀態仍為 `HUMAN_TEST_PENDING`。
