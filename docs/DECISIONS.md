# DECISIONS｜重大設計決策

只記錄真正影響後續開發的重要決策，不與 CHANGELOG、PROJECT_STATE 混用。

## ADR-000｜採用最小可用 AI Agent 治理
- 日期：2026-10-11
- 情境：跨多種 Agent 和專案反覆重建規則，且規則可能互相衝突。
- 決策：以精簡 AGENTS.md 定義安全底線，另以 facts、state、decisions、review、deployment 承載各自職責。
- 原因：避免龐大且不易更新的單一 Prompt。
- 影響：需要隨專案配置現實部署與驗證命令，不能預設全部可自動化。
- 來源：agent-platform、agent-governance-bootstrap、agent-development-loop 既有治理原則。


## ADR-001｜GitHub 保護規則狀態採時點確認並逐輪重查
- 日期：2026-10-19
- 情境：Rulesets API 因方案限制回 403，先前因此把保護規則及 Required Checks 長期留在 UNKNOWN。
- 決策：維護者確認指定的三個 Repo 當日沒有 Branch Protection 或 Rulesets；該時點狀態記為 NOT CONFIGURED，GitHub Required Checks 也記 NOT CONFIGURED，而不是 PASS。這個觀測不代表未來設定不會改變。
- 邊界：每個後續 PR 都要重新發現目標分支保護規則、Rulesets 與 Required Checks，遵守新發現設定。403 只記錄 API 觀測限制；若缺少當輪有效維護者確認及可稽核證據，才保留 UNKNOWN 並提出最小補證，不得因同一 403 無限重試。
- 合併授權：本範本 docs/DEPLOYMENT.md 仍禁止 Auto-merge。Repo Auto-merge 功能設定、Agent 單次授權與帳號手動 merge 權限分別核驗；本 ADR 不授予任何合併、部署或設定變更權限，也不放寬內部必要驗證 Gate。
