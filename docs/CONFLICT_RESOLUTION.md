# CONFLICT_RESOLUTION｜來源去重與優先序

## 本範本比對的既有來源
- `agent-platform/AGENTS.md`、`templates/consumer/AGENTS.md`：Shared Skill canonical、manifest exact-match、安全預檢。
- `agent-governance-bootstrap/MINIMUM_GOVERNANCE.zh-TW.md`：最小治理、Context／Authority／Evidence／Verification 分層。
- `agent-governance-bootstrap/GOVERNANCE_BOOTSTRAP_PROMPT.zh-TW.md`：探索、提案及實作分階段，未知不可編造。
- `agent-development-loop/AGENTS.md`、`README.md`：目標導向、Reviewer 分離、遠端自動化不得虛構。
- `bento-order-app/AGENTS.md`：中文版流程、變更紀錄＋繁中翻譯與正式 Worker 部署特例。
- 使用者之前 AI 協作簡報的架構方向：AGENTS 共用契約／Copilot adapter／scoped instructions／architecture／decisions／editorconfig。此處以找到的實際 GitHub 文件作為可核對的逐條規則來源。

## 解決方式
| 差異／重複 | 範本決定 |
| --- | --- |
| Copilot instructions 與 AGENTS 規則重複 | Copilot 檔僅作入口，權威在 AGENTS |
| 巨型 AGENTS 與 Minimum Viable Governance | AGENTS 僅保留持久安全及流程，狀態、事實、決策、部署分檔 |
| agent-platform 必須有 manifest vs 空白通用專案 | 僅使用 Shared Skills 的 consumer 需要合法 manifest、版本與快照；不假裝所有新 repo 都已安裝 |
| 通用的禁止未授權部署 vs Bento 預設 deploy | 通用預設禁止；已經明確授權的專案可在 DEPLOYMENT 定義窄化例外 |
| 版本更新 vs 純文件變更 | 使用者可感知／正式發布才檢查升版；正式版翻譯必須同步 |
| GitHub Actions 每五分鐘 vs 雲端 AI 自動開發 | 排程協調不等於 AI 可啟動；未驗證 P0 capability 前不能宣稱無人值守 |
| Reviewer 與 Developer 的自評 | Reviewer 獨立核對 head SHA 和原始證據；改動後重查 |
| Share Skills 的版本與複製文件 | 上游 agent-platform 是 canonical；新 repo 不複製虛構或過期的 snapshot |

## 優先順序
1. 使用者當次明確要求及更嚴格的安全／平台限制。
2. 根 `AGENTS.md` 持久安全底線。
3. 經證實的專案專屬政策（`docs/DEPLOYMENT.md`、`REPO_FACTS.md`），只能在合法邊界內窄化授權。
4. `agent.yaml` 與 pinned shared skills 的技術契約（如果使用）。
5. Copilot adapter、scoped instructions、一般指引。

發現無法調和的衝突，先停止有外部副作用的操作、留下證據並要求決策；不要默默選擇最寬鬆政策。
