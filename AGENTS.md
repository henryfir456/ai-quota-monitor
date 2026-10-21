# AGENTS.md

本檔是本 Repository 的根層入口。`AGENTS.md` 是 Markdown 指引；格式本身沒有必填欄位，具體支援範圍請看 [`docs/agent-adapters/README.md`](docs/agent-adapters/README.md)。

## 必讀規範

- 共用且可執行的 Agent／阻塞／驗證政策：[`docs/policies/shared-agent-policy.md`](docs/policies/shared-agent-policy.md)。此檔是 Template 與 ADL 的共用政策來源。
- Template 專屬安全、Issue/PR、驗證、部署及權限政策：[`docs/policies/agent-workflow.md`](docs/policies/agent-workflow.md)。此檔保留原根規範的詳細要求。
- 技術棧與實際命令：[`docs/REPO_FACTS.md`](docs/REPO_FACTS.md)；隱私、部署與衝突處理另見 `docs/PRIVACY_PUBLICATION_GATE.md`、`docs/DEPLOYMENT.md`、`docs/CONFLICT_RESOLUTION.md`。

## 共通底線

- 開工前確認 Repo、分支、任務授權及既有修改；不得覆蓋其他工作階段的變更。
- 遵守共用政策和最近的適用 `AGENTS.md`；若局部指示衝突，依 `docs/CONFLICT_RESOLUTION.md` 處理並保留更嚴格的安全限制。
- 不得猜測 Agent、外部 API、CI、Required Checks 或權限狀態；缺證據標示 `UNKNOWN`，並依有界阻塞流程記錄和轉做獨立工作。
- 不得由訂閱推定派工 API 可用，不得模擬登入、要求明文 Token、繞過安全 Gate 或新增未授權付費服務。
- 對外內容使用繁體中文；實際程式修改須評估 Changelog、測試及回顧要求。

## Windows 可執行檔 Release 強制驗證（阻擋式 Gate）

- 凡輸出 Windows EXE、MSI、MSIX 或桌面安裝包的專案，最終打包成品必須先在 GitHub Actions Windows Runner 上**實際啟動並通過驗證**，才允許建立或發布正式 Release。編譯、單元測試與檔案存在檢查不可替代啟動測試。
- 工作流程必須是 build → package → Windows smoke test → SHA-256 驗證 → Release；Release 必須依賴 smoke 與雜湊工作成功，並沿用同一最終成品。禁止驗證後重新建置／重封裝未測試檔案；不得以 `continue-on-error`、`if: always()`、條件跳過或手動旁路跳過 Gate。
- 在乾淨 Windows Runner 啟動交付 EXE，以有界等待驗證進程未立即退出、無未處理 .NET／Application Error，且主視窗或預期 GUI／系統匣功能確實初始化；僅進程短暫存在、僅編譯成功均不算 PASS。應有不依賴真實帳號、Token 的安全測試模式及 ready 訊號。
- WPF 專案必須覆蓋主視窗 XAML、Binding、資源初始化。測試須能偵測程序崩潰、XamlParseException 與啟動即退出，並含故意失敗的反向案例確認發布被阻擋；清理測試進程及暫存資料。Smoke 模式只能跳過外部 Provider／帳號／Token，不可跳過要驗證的視窗與控制項。
- 結果為 FAIL、UNKNOWN、取消、逾時或缺少測試證據時，標記 `RELEASE_BLOCKED`，不得建立正式 Release。Windows Runner 無法使用時可留內部測試成品，但不可對外聲稱已驗證。
- 每次 Release 保留 Runner OS、測試指令、ready/窗口檢測結果、例外摘要、最終 SHA-256 與 Actions run 連結，不可輸出憑證或敏感資訊。
- Agent 必須同步落實實際 CI workflow、可執行 smoke-test 程式與反向測試；CI 要在 Release 前執行且對測試失敗 fail closed。模板提供可重用 Gate 與 CI 合約，採用專案須在自己的 workflow 對實際最終 EXE 接線並檢查其 UI ready 訊號；**僅修改 AGENTS.md／README 或僅測試假畫面並不能宣稱 Release Gate 已生效**。遵循 repo 原有 Issue／PR／Review 流程。
