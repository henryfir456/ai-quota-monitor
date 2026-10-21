# AGENTS.md｜AI 開發共同契約（繁體中文）

本檔適用於本 Repository。請使用繁體中文撰寫回覆、Issue、PR 與變更紀錄。此檔是持久的安全和協作規則，不是專案日誌，也不是萬用 Prompt。

## 公開紀錄個資保護（強制）
任何 Issue、PR、commit、review、測試資料、回顧、日誌及截圖在寫入 GitHub 前，必須遵守 [../PRIVACY_PUBLICATION_GATE.md](../PRIVACY_PUBLICATION_GATE.md)：預設使用完全虛構代號與測試資料，禁止真實姓名、完整或可反查員編、LINE 識別、憑證及關聯敏感資訊；檢查未通過時阻擋發布與合併。Agent 開發 Loop 每階段執行 PII_SCAN，Reviewer 獨立覆核，歷史殘留另作事件處理。


## 1. 開工前
1. 確認 Repository、預設分支、任務授權、`git status --short`、既有 staged / unstaged / untracked 修改。
2. 閱讀 `../REPO_FACTS.md`、`../PROJECT_STATE.md`、`../DEPLOYMENT.md` 和相關架構／決策；標示尚未查證的資訊為 UNKNOWN。
3. 有 `agent.yaml` 時依其宣告驗證 schema、Skill ID／路徑／版本；不可自行用 global、parent 或 latest Skill 替代。沒有 manifest 的專案可使用本範本原生流程，不可假裝已完成 Shared Skill 驗證。
4. 沿用現有架構、命名、logging、測試方式。修改必須有界；不做無關重構。
5. 若專案已有更嚴格的安全限制，一律保留。專案特例不得繞過共用安全底線。

## 2. 任務生命週期
`INTAKE → PREFLIGHT → CONTEXT → PLAN → IMPLEMENT → VERIFY → INDEPENDENT REVIEW → HANDOFF / DONE`
- INTAKE：釐清真實目標、成功條件、明確不做的事、授權範圍。對複雜工作按依賴拆 Issue。
- PLAN：列出 in/out of scope、影響範圍、風險、測試、交付方式；大改動先凍結計畫，重大漂移須重新確認。
- IMPLEMENT：保護既存使用者修改；不 reset、clean、覆寫或破壞無關工作。可使用獨立分支或 worktree。
- VERIFY：記錄 PASS／FAIL／NOT RUN／NOT VERIFIED，附命令、實際輸出與環境；人工實機及正式環境測試必須另外標示。
- REVIEW：開發者不能代替獨立審閱者。Reviewer 檢查需求、實際 diff、CI、權限、風險及 PR head SHA。
- HANDOFF：保存目標、狀態、提交與檔案、測試證據、風險及下一步。只有達到專案明定的交付門檻才能宣稱 DONE。

## 3. 執行權限
- GREEN：唯讀檢查、安全的本地測試及有界修改；對外寫入仍要符合使用者本次指示。
- YELLOW：依賴升級、權限／認證、CI、共享 API、重大架構調整；明示風險、確認範圍，無法確認即停。
- RED：正式部署、資料異動／刪除、破壞性 migration、Secrets、權限升級、force push、自動合併與無法回復的外部動作，必須由使用者明確授權或有可核對的專案既有授權政策。
- `../DEPLOYMENT.md` 是專案部署政策的記錄位置；未完成或只有 placeholder 時，視為**未授權**。命令出現在文件／manifest 不等於執行授權。
- 共用 Skills 的快照不得隨意直接改寫；需要升級時回到 canonical `agent-platform` 及版本化同步流程。

## 4. 版本與翻譯
- 每次使用者可感知的功能或修正，須評估是否升版並更新 `CHANGELOG.md`。
- 正式升版時，使用的版本來源、CHANGELOG 和**專案實際存在的**繁體中文 UI release/changelog 資料必須同步；缺翻譯禁止發布，不得吞錯或以空內容規避。
- 具體程式檔案與驗證命令由 `../REPO_FACTS.md` 宣告。純文件或內部治理變更不必虛增版本，但須說明不升版原因。

## 5. 交付、阻塞與費用
- 分支、Draft PR、mergeable、CI PASS 均不能單獨代表已合併／已部署／實機驗證。
- 依 `../DEPLOYMENT.md` 查核正確的部署對象、是否需前後端分別部署及 smoke test；沒有部署證據不能宣稱線上修復。
- 記錄阻塞原因、已做的嘗試與證據；同一修正原則上最多三輪，避免無效迴圈。其他不受影響工作可繼續。
- 不得繞過 AI 產品訂閱限額、虛構外部 Agent 已啟動，或未經授權新增付費 API／服務。
- 專案規則無法覆蓋安全底線。發生衝突時依 `../CONFLICT_RESOLUTION.md` 處理。


## 6. 跨語言／平台規範發現
1. 先讀 `../REPO_FACTS.md` 確認實際技術棧、執行平台、編譯器／Runtime 版本及驗證命令；未知標記 UNKNOWN。
2. 再依 `../standards/INDEX.md` 的檔案副檔名及執行平台讀取所有相關規範；混合語言不可只套用主要語言。
3. `.editorconfig` 與 `.gitattributes` 是可機械檢查的格式基線，CI 才是驗證依據；文件規則不可取代實際 Parser、Lint、Test 或 Build。
4. 新語言若缺少規範，先識別官方相容性、既有慣例及目標執行環境，補充最小規範與驗證，不能推定預設設定適用所有平台。
5. 雲端 Agent 不可假設可存取本機上層資料夾；未實際取得的外部規範不可宣稱已遵循。

## 已執行的驗證證據 Gate（2026-10-15）
- 測試腳本存在、靜態搜尋、Code Review 與生成程式碼不等於測試已執行；記錄 PASS／FAIL／NOT RUN／BLOCKED，附目標 runtime 版本、實際命令、退出碼及輸出證據。
- 必須辨識受影響的語言及目標平台；Windows PowerShell 5.1 的 .ps1 必須由對應 Parser 及啟動檢查驗證，編碼需遵守 UTF-8 BOM，.bat 必須檢查失敗時正確非零結束。
- 必要 Parser／Lint／Build／單元測試任一未執行或失敗均阻擋完成與自動合併，不得以 CI 額度不足、已建立測試或預定人工實測代替；環境缺少時標記 BLOCKED 並提供解阻途徑。
- 僅人工真機／真實環境驗收可在 main 合併之後，前提是合併前必要自動 Gate 已取得成功證據。獨立 Reviewer 必須檢視驗證產物，不得只看測試檔。

## 每輪開發回顧（必填）
每次有實際修改的交付前，依 `../retrospectives/README.md` 建立／更新該任務回顧：本輪範圍與結果、實際阻礙與證據、嘗試及根因信心、驗證結果、遺留項目、優先級、改善建議與下次預防。無阻礙填「無」，未查證原因填 UNKNOWN，不得編造證據。開工前查閱相關既有回顧，避免重複錯誤。只將具追蹤價值且非重複的改善建立 GitHub Issue；涉及權限、正式部署、自動合併仍依原有授權與 Gate，不因記錄回顧而自動執行。

## 外部服務 Readiness 與證據分級 Gate（強制；2026-10-18）
- 對網路、Secrets、身分／憑證、工具能力、外部 API 操作分別記錄 `PASS`、`FAIL`、`UNKNOWN`、`BLOCKED` 或 `NOT RUN`，每項附觀測來源、時間／revision、實際命令或請求結果。不得將 `UNKNOWN`、未執行、快照存在、設定已保存或工具缺席寫成 `PASS`／憑證失敗。
- 觀測狀態 `unknown`、環境變數 `unknown` 代表**沒有足夠當前證據**，不等於已失效；只有有界的已執行認證請求遭明確拒絕，才能判定相應憑證驗證 `FAIL`。過去 `ready` 不可直接替代最新 `unknown`。
- 對控制面政策需確認觀測仍為 current、spec/observed revision 一致及所需 `restricted/enforced` 狀態；執行器快照顯示 `restricted` 或 allowlist 有目的地主機，不等於已確認控制面 `enforced`。無法確認時 Gate 保持 `BLOCKED`，只做安全唯讀診斷，不得放寬為 Unrestricted、使用萬用字元或擴大代理／網域權限。
- 先辨識必要能力及可替代工具。缺 AWS CLI／boto3 並不等於無法驗證 S3，也不等於憑證錯誤；可評估既有 `rclone` 等 S3 相容工具。工具不存在、不能執行或能力不足，分類為 `TOOLING_BLOCKED`，列出安全備援與實際證據，不偽造 API 結果。
- **前置 Gate fail-closed**：尚未證明變數可用、網路策略符合政策、精確目的地主機獲允許及工具可用之前，不得進入具狀態的外部驗證。通過後仍先使用不含個資的合成資料，在授權範圍內執行寫入、讀回、完整性比對與有界清理；各步分別判定，不能以「沒有建立物件」冒稱刪除操作成功。
- Secret 只能透過受控環境注入；禁止將密鑰、簽名請求、完整敏感主機識別、個資或可還原識別資料寫入 GitHub Issue／PR／log／回顧。必須保留既有 PII_SCAN、Reviewer 獨立審查、Required Checks、權限、合併及部署政策。新增驗證規則不授權正式資料匯出、migration、資料刪除或生產部署。
- 遇 `UNKNOWN` 應報「待證實的觀測」與最小補證動作；遇 `BLOCKED` 應報具體阻塞、責任端及安全解阻途徑；只有實際執行且取得明確結果才可升格 `PASS`／`FAIL`。將重複出現的阻塞、替代工具決策及預防措施回寫既有 retrospective，並評估同步 ADL 與共用範本，避免孤立補丁。

## PR 證據補齊與有限解阻流程（強制；2026-10-19）
- **先修復可修復阻塞**：遇到回顧缺漏、PR 缺連結、文件檢查未執行、CI 未觸發或外部服務異常，不得只回報 BLOCKED 就停止；應在既有授權內依序蒐證、補件、執行適用測試、要求獨立重新審查。每輪記錄精確 PR head SHA、證據 URL、命令／退出碼、觀測時間、責任端；同一根因最多三次有新證據的嘗試，禁止無效輪詢。
- **回顧自動補件**：有實際修改但缺少 retrospective 時，依本庫 `../retrospectives/README.md` 建立／更新該輪文件，在 PR 說明連結；不得以空白模板、推測根因或虛構測試結果充數。回顧為合併前 Gate，補件本身不能取代測試或審查。
- **分清檢查狀態**：將 GitHub Required Checks、Actions workflow、Commit Status、外部 Preview Build、本地／備援測試分別列帳，使用 PASS／FAIL／UNKNOWN／BLOCKED／NOT RUN／NOT APPLICABLE；沒有 Actions run、路徑篩選未觸發、GitHub API 沒有 statuses，都不是 CI PASS 或 FAIL。僅在核對實際設定與本次變更影響面後才能記 NOT APPLICABLE。
- **GitHub 規則查詢受限**：Rulesets 或 Branch Protection API 回傳 403，記錄原始錯誤類型（權限不足、功能方案限制等），改由有權限的 GitHub Settings 畫面、管理員／可稽核的設定輸出確認現行合併政策；無法取得時保留 UNKNOWN 與具體補證要求。不得因 API 403 直接推定沒有 Required Checks，也不得為解阻擅自更改保護政策、Repo 可見度、方案或權限。
- **外部 Preview／Build 失敗**：先取得失敗 build ID、來源 commit、完整可安全公開的診斷摘要，確認與本 PR 的關聯及該檢查是否為必需。相關則修復並重驗；無關的既存失敗也須有可重現證據與獨立 Reviewer 核准，只有合併政策允許才可記錄不阻擋。不得以「只有文件變更」逕行忽略真正的 Required Check；正式部署另遵守原授權與 Gate。
- **CI 額度與備援**：只有明確的額度錯誤才記 QUOTA_BLOCKED；得使用已核准、可重現的 Codex／本地備援進行相同範圍驗證，記錄其執行環境及證據，但不得冒充 GitHub Required Check 通過、規避計費或訂閱限制，亦不得減少既有必要測試。
- **精確 head 與合併**：任何提交改變 PR head 後，所有受影響 Gate 重新對最新 SHA 執行；獨立 Reviewer 檢查新 diff、原規範不弱化、PII_SCAN 與權限證據。只有適用必要測試、真實 Required Checks、獨立審查及本庫合併授權均滿足時，才可標記 Ready／合併；禁止因 GitHub 顯示 mergeable 就宣稱可安全合併。未授權正式資料、Secrets、Worker／Netlify 部署一律不執行。
- **解阻回報格式**：逐項列「Gate／狀態／精確證據／本輪已採取的解阻動作／仍缺什麼／有權處理者」；未知與未執行不得寫為 PASS。若仍阻擋，留下下一個可執行且最小授權的步驟，而非泛稱重試。對跨專案共通根因，評估同步 ADL 與 Template，保留差異化部署政策。

## GitHub 保護規則、Required Checks 與合併授權（強制；目前狀態確認 2026-10-19）
- 維護者於 2026-10-19 明確確認 agent-development-loop、agent-project-template、bento-order-app 目前都沒有 Branch Protection 或 Rulesets。對本次 PR 核驗，兩者均記為 NOT CONFIGURED。同日 main 分支摘要亦顯示 protected=false、Required Status Checks enforcement 為 off 且 contexts/checks 為空；Rulesets API 回傳空清單或因方案限制 403 的差異要如實保留。403 是該 API 的可觀測限制，不與本次維護者確認矛盾，也不得再單獨造成無限期 BLOCKED；若出現其他有效且較新的相反政策證據，先釐清後再合併。
- 透過 GitHub 保護規則設定的 Required Checks 狀態為 NOT CONFIGURED，絕不能記成 PASS。它與 Repo 內部必要 Gate 分開列帳；適用 Parser、Lint、Test、Build、PII_SCAN、獨立 Reviewer、權限、合併及部署安全規則仍然強制，任一必要 Gate 未通過仍阻擋合併。
- 此政策事實只代表 2026-10-19 的確認快照，不是永久假設。每個未來 PR 任務都要重新查詢目標分支 Branch Protection、包含 inherited rules 的 Rulesets、實際 Required Checks 與目前 Repo 設定；新發現的規則立即適用。若查詢回 403，記錄錯誤且最多做一次有意義的替代唯讀查核；本次有效維護者確認適用時依確認分類，否則保留 UNKNOWN 並列最小補證方式，不得由 403 推論 PASS 或規則存在。
- 必須分開記錄三件事：Repo 的 allow_auto_merge 只是 GitHub Auto-merge 功能是否啟用；Agent 是否獲准合併取決於本次明確的使用者授權及專案政策；手動合併另需目前 GitHub 身分具備 merge 權限。設定、授權或權限任一項都不能代替另外兩項。手動合併只在本次授權涵蓋、權限可查證且所有 Gate 通過時執行；不得因 mergeable、Auto-merge 設定或 reviewer 通過而推定授權，也不得自行啟用 Auto-merge。
- Template 專案另受 ../DEPLOYMENT.md「自動合併授權條件：未授權」約束；本節不變更此政策、不啟用 GitHub Auto-merge。明確的單次使用者授權只可能涵蓋該 PR 的手動合併，仍須符合其他所有 Gate 與 GitHub 手動 merge 權限，不會轉成持續授權。
