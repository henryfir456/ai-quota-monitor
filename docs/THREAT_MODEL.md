# 威脅模型

更新日期：2026-10-21。範圍為 Windows 11 使用者工作階段中的 v0.1 Monitor、Codex app-server 呼叫、可選 Claude statusline bridge 和本機偏好檔。

## 資產

- 使用者由 Codex 管理的登入憑證（Monitor 不讀取其檔案或記憶體）。
- 真實額度百分比、reset 時間，以及 statusline 原始 JSON 中可能存在的 session／工作目錄資料。
- Monitor 設定與 portable 程式檔。
- 發行檔、依賴和 Windows CI 結果。

## 信任邊界與資料流

1. WPF UI → Provider：僅傳 `ProviderSnapshot`，不顯示 raw RPC／statusline 字串。
2. Monitor → Codex CLI：以絕對路徑啟動使用者現有的 `codex.exe app-server`，只傳標準輸入 JSON-RPC 初始化及額度讀取；子程序環境採 OS 路徑 allowlist。Codex app-server 自行管理其認證和網路行為。
3. Claude Code → helper → named pipe：Claude 依使用者手動設定執行 helper；helper 先從 stdin JSON 挑出 5 小時與 7 日 rate limit 欄位，再嘗試連到 `CurrentUserOnly` pipe。沒有 Monitor 時不保存資料。
4. Monitor → `%LOCALAPPDATA%`：只保存使用者可調整的非敏感偏好，採原子檔案替換，不保存原始來源回覆、Token 或帳號識別。
5. 原始碼 → GitHub Actions：CI fixture 全為合成資料；Action 版本使用完整 commit SHA pin。

## 威脅、緩解與殘餘風險

| 威脅 | 緩解 | 殘餘風險／狀態 |
| --- | --- | --- |
| Codex RPC malformed response 造成錯誤額度或洩漏 | duration/percentage/reset 嚴格驗證；未知欄位 UNKNOWN/N/A；不記錄 raw response／stderr | Codex app-server 是實驗性協定，上游 schema 可能變更；Windows CLI matrix 仍待實機 UAT |
| 子程序繼承 API key 或任意環境設定 | 清空繼承環境，只複製 OS 路徑及使用者 profile 必需欄位；不讀 credential files | 使用者本機的 Codex 安裝自身信任其既有登入狀態 |
| Claude JSON 含 session ID、transcript path 或專案路徑 | helper 只生成 rate_limits allowlist 最小 JSON，Monitor 不讀完整原始 stdin | 使用者需自行選擇配置 command；自訂既有 statusline 需手動合併 |
| 其他本機程序偽造 Claude named-pipe 內容 | pipe 設 `PipeOptions.CurrentUserOnly`，資料仍通過百分比／reset parser | 同一 Windows 使用者的惡意程序仍可連線或偽造 UI 數字；pipe 不是跨同一帳號的身分隔離 |
| UI 資料被當成可信數字 | DTO 限制有限值 0～100；狀態與來源顯示；不以錯誤文字替代數字 | 真實來源端若提供有效但錯誤的 quota，Monitor 無法獨立校正 |
| 設定檔被替換或損壞 | schema version、讀取驗證、失敗時回退預設值；原子寫入 | 同使用者權限可修改設定；不保存 secrets，因此無憑證保護需求 |
| 未知／脆弱 NuGet 或 GitHub Action 依賴 | 套件固定、NuGet audit 嚴格告警、Action SHA pin、Gitleaks | 每次 Release 仍需檢查新安全公告和實際 Windows 產物 |
| 假冒 Release／未簽章二進位 | Release 附 SHA-256SUMS、commit/tag、SBOM/依賴清單及 unsigned notice | SHA-256 不驗證發行者；本版本無程式碼簽章 |

## 明確不做

讀取 OAuth Token、呼叫非公開 Claude OAuth API、收集 session/transcript、執行推論、自動登入／更新／修復／安裝 CLI、背景遙測或遠端更新。
