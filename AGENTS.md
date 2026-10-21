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
