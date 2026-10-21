# REVIEW｜獨立 Reviewer Gate

Reviewer 需與該輪 Developer 分離；即使同模型不同 Session，也須獨立取得 PR diff、需求、CI 及相關環境證據，不能只重述 Developer 自評。

## 審閱輸入
- Issue 目標、邊界、驗收條件及明確授權。
- PR base/head SHA、實際 diff、未完成項。
- 自動驗證、實機驗證、版本／翻譯對照。
- 部署政策和資料安全風險。

## 輸出
- `APPROVE`：同一 head SHA、所需 gate 通過且沒有 blocker。
- `CHANGES_REQUESTED`：列出具體阻塞、位置與可驗證修正。
- `BLOCKED`：缺權限、證據或環境，不能臆測。
- 改變 head SHA 後必須重新評估審閱與 CI 是否仍有效。

Reviewer 通過≠允許自動合併、部署。專案交付門檻以 AGENTS.md 和 DEPLOYMENT.md 的有效授權為準。


## GitHub 保護規則、Required Checks 與合併

- 2026-10-19 維護者確認三個指定 Repo 當前皆無 Branch Protection 或 Rulesets；對此日期的快照標示 NOT CONFIGURED。透過保護規則設定的 GitHub Required Checks 同樣是 NOT CONFIGURED，不能寫為 PASS。這不取消 Repo 內部必要的 Parser、Lint、Test、Build、PII_SCAN、獨立審查、權限與部署 Gate。
- 每個後續 PR 必須重新查目標分支 Branch Protection、Rulesets（含 inherited）、Required Checks 及實際 workflow/check-run；新發現的政策立即適用。本次確認不是永久預設。API 403 需記錄為可觀測性限制；若沒有當輪有效維護者確認或可稽核設定，採 UNKNOWN 並列最小補證方式，不得重複無效查詢。
- 將 Repo allow_auto_merge、Agent 的單次合併授權、以及目前帳號的手動 merge 權限分開判斷。GitHub Auto-merge 啟用不代表 Agent 獲准；關閉也不會自行撤銷已明確授權且有權限的手動合併。
- 本範本 docs/DEPLOYMENT.md 的 Auto-merge 條件仍為「未授權」。本流程不啟用 Auto-merge，也不修改該政策。單次明確授權只可能涵蓋指定 PR 的手動合併，且仍需所有適用 Gate 通過。
