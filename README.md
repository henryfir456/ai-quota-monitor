# AI 專案開發範本

這是一套可重複使用的 GitHub 專案起始範本，採用繁體中文、最小可用治理及證據導向的 AI Agent 開發流程。

> 建立新專案後，先完成 `docs/REPO_FACTS.md`、`docs/PROJECT_STATE.md`、`docs/DEPLOYMENT.md` 的專案設定；範本本身**不授權正式部署或自動合併**。

## 使用方法
1. 在 GitHub 將本儲存庫設為 Template repository，之後使用 Use this template（或手機 Start with a template）建立新儲存庫。
2. 確認新儲存庫的資料可見性、預設分支及 Secrets；範本不會複製原始碼以外的設定、權限及 Secrets。
3. 由 Agent 先唯讀盤點專案，再修改專案特定設定與驗證指令。
4. 依 `AGENTS.md` 執行需求釐清、Issue、計畫、實作、驗證、獨立審閱與交接。

## 文件導覽
- `AGENTS.md`：共用指令入口，連結到可維護的細節政策。
- `docs/policies/shared-agent-policy.md`：ADL 與 Template 共用的阻塞、證據及交付政策來源。
- `docs/agent-adapters/README.md`：Codex、Claude 與 Copilot 的載入差異及限制。
- `scripts/sync-shared-agent-policy.cjs`：只同步共用政策快照與 adapter；預設唯讀檢查並拒絕未知 Repo。
- `.github/copilot-instructions.md`：Copilot 轉接規則，不重複另訂權限。
- `docs/REPO_FACTS.md`：已驗證的技術事實、命令及路徑。
- `docs/PROJECT_STATE.md`：目前進度、阻塞與下一步。
- `docs/DECISIONS.md`：持久設計決策。
- `docs/ARCHITECTURE.md`：架構與系統界線。
- `docs/DEPLOYMENT.md`：專案部署授權及回滾契約。
- `docs/REVIEW.md`：獨立審查標準。
- `docs/CONFLICT_RESOLUTION.md`：來源差異、去重及優先順序。

## 原則
GitHub 是程式及 Issue/PR 狀態的持久來源，驗證要有可稽核證據；絕不以 PR 數量、CI 綠燈或對話宣稱替代使用者目標的真正完成。共用 Skill 仍由 `agent-platform` 管理，不直接複製可能過期的快照。未實際配置的 AI 雲端自動派工一律視為未啟用。
