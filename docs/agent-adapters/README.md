# Agent 指令載入相容性

此矩陣說明工具如何載入政策入口，不替工具能力背書；產品版本、設定與執行介面會改變支援範圍。工作時記錄實際 Agent surface 和版本；本 Repo 沒有對所有雲端／IDE surface 做 runtime 測試。

| Surface | 已知指令入口 | 本 Repo adapter | 限制／驗收方式 |
| --- | --- | --- | --- |
| Codex CLI | 從設定目錄及 Repo root 到目前工作目錄發現 `AGENTS.md`；較近目錄可細化規則 | 根 `AGENTS.md` | 每次在實際工作目錄確認載入結果；本機父目錄規範不可假設在雲端 checkout 可見。Codex Cloud／其他 hosted surface 需按當前文件及實際 checkout 分別確認。 |
| Claude Code | 官方記憶文件以 `CLAUDE.md` 為專案入口，支援 `@AGENTS.md` 匯入；本次不推定所有版本或 surface 會直接發現 `AGENTS.md` | 根 `CLAUDE.md` 以 `@AGENTS.md` 匯入共同規則 | 在目標 Claude Code 版本檢查 import 是否解析；未測版本／surface 標記 NOT VERIFIED，不複製政策。 |
| GitHub Copilot | 支援因 IDE、Copilot coding agent、GitHub code review surface 而異 | `.github/copilot-instructions.md` 只作共同政策入口 | GitHub.com code review 使用 repo-wide Copilot instructions；不要推定該 surface 會讀 `AGENTS.md`。IDE 與 cloud agent 依 GitHub 當前支援矩陣核對。 |

## 共用政策入口

- `AGENTS.md` 是共同 Markdown 根指令；Claude 與 Copilot adapter 只轉送，不另訂安全、驗證或 merge 規則。
- 詳細共用政策以 [`../policies/shared-agent-policy.md`](../policies/shared-agent-policy.md) 為唯一來源；Template 專屬規則見 [`../policies/agent-workflow.md`](../policies/agent-workflow.md)。
- 本 Repo 沒有子目錄 `AGENTS.md`。只有在某個子專案需要更窄且不同的規則時才增加，並確認該工具支援該範圍。

## 官方產品文件

- [agents.md 格式與慣例](https://agents.md/)
- [Codex prompting guide](https://developers.openai.com/cookbook/examples/gpt-5/codex_prompting_guide)
- [Claude Code memory and instruction files](https://code.claude.com/docs/en/memory)
- [GitHub Copilot custom instructions support matrix](https://docs.github.com/en/copilot/reference/custom-instructions-support)
- [Copilot repository instructions](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/add-custom-instructions/add-repository-instructions)
