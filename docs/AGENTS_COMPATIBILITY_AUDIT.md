# AGENTS.md 與跨 Agent 閉環審查

檢查日期：2026-10-20。遠端基線：ADL `f3f56939df68e57516c74c1c4493b61d75851ab7`；Template `4e87a49e611b5b658eb06578e8a39e338ad722c9`。本文件分清官方文件、產品支援和本專案政策；提案分支上的修正不代表已進入 `main`。

## A. agents.md 官方格式與慣例

官方網站將 `AGENTS.md` 定義為供 coding agent 使用的一般 Markdown 指引，沒有必填欄位或強制標題。官方建議說明專案概觀、建置與測試、程式風格／測試、安全及 PR 慣例；規範可以位於 Repo root，monorepo 可用巢狀檔案收窄範圍，較近的適用檔案優先。官方網站沒有定義自動派工 API、retry 狀態機、merge 授權、quota fallback 或 UAT 時機。

兩個 Repo 在基線都有 root `AGENTS.md`，是普通 Markdown，亦含技術命令、安全及審查政策；無格式性缺漏。它們各是一個單一用途 Repo，沒有需要不同指示的獨立子專案，因此目前不需要新增巢狀 `AGENTS.md`。此結論是依現有目錄結構的選擇，不是官方禁止巢狀檔案。

## B. Agent 工具實際支援範圍

產品能力依 surface、版本及設定而不同。採用同一份 `AGENTS.md` 作政策來源，再用 Claude／Copilot 入口轉接；不把特定工具規則當作官方 AGENTS 標準，也不假設本機父層規則會出現在 hosted checkout。詳見 [`agent-adapters/README.md`](agent-adapters/README.md)。

| Surface | 目前文件支持的載入行為 | 本次驗收狀態 |
| --- | --- | --- |
| Codex CLI | 發現 Repo root 到目前工作目錄的 `AGENTS.md`，較近檔案可細化規則 | 文件確認；本次未執行 Codex runtime 載入測試 |
| Codex Cloud／其他 hosted Codex | checkout、上層路徑與能力取決於 hosted environment | 未驗證派工、Agent 結果回呼或自動 merge |
| Claude Code | 官方記憶文件以 `CLAUDE.md` 為入口並支援 `@AGENTS.md` import；不推定所有版本或 surface 會直接發現 `AGENTS.md` | Adapter 已加入；本次未在 Claude Code runtime 測試 |
| GitHub Copilot | IDE、coding agent 與 GitHub code review 的支援矩陣不同；GitHub code review 主要使用 `.github/copilot-instructions.md` | Template 與 ADL adapter 已加入；本次未測各 Copilot surface |

## C. 本專案工程政策

以下均是 Template／ADL 自訂政策，不是 agents.md 官方要求：Issue lifecycle、阻塞分類與重試上限、證據綁定 SHA、quota fallback、獨立 reviewer、PII scan、Required Checks、人工 UAT 時點、merge／deployment 授權與 audit log。Template 的 [`policies/shared-agent-policy.md`](policies/shared-agent-policy.md) 是共用來源，ADL 同路徑為同步快照；Repo 專屬安全及交付規則保留在各自的 `policies/agent-workflow.md` 與既有部署／隱私文件。

## 問題清單與驗收

| ID／優先級 | 影響範圍與基線問題 | 修正方式 | 驗收條件／本分支狀態 |
| --- | --- | --- | --- |
| A-01 · P0 | ADL 尚無經驗證的 Codex Cloud／ChatGPT Work coding-agent dispatch 與結果回寫介面。訂閱狀態不提供 API 證據。 | dispatcher 維持 fail-closed；只在取得正式授權介面、事件驗證與隔離測試 Repo 後才接入。 | 真實 sandbox workflow 從 Issue 派工到 same-head 測試、獨立 review、合規 merge、狀態回寫及下一 Issue，保留每階段證據。**目前未滿足；未宣稱 unattended development 完成。** |
| A-02 · P1 | ADL root `AGENTS.md` 引用不存在的 `docs/REPO_FACTS.md`，使建置／測試指令無單一可驗證索引。 | 補 Repo Facts 並讓 root 入口指向它。 | Facts 列出 runtime、check/test、controller、workflow 與限制；檔案及命令可在本 Repo 解析。**本分支已補。** |
| A-03 · P1 | 兩 Repo 的阻塞文字分散於 root、operations、retrospective 及早期 PR，沒有可由引擎執行的共用八類 matrix；quota、UNKNOWN 調查及人工 UAT容易變成重跑或全部停工。 | 建立共用政策；ADL 以純函式判類、SQLite 原子保存 UNKNOWN 次數，第三次後明確 exhausted 並繼續輪替其他 Issue。 | 八類都有證據欄位、重試、備援、merge 條件及下一步；未知分類同 Repo／Issue／原因最多三筆 audit，並行時也不超限；其他候選仍可選。**ADL 全套本機測試 164/164 PASS，Template 31/31 PASS；GitHub Required Checks 仍待 PR 階段即時查核。** |
| A-04 · P1 | Template 的 `REPO_FACTS.md` 全部為 `UNKNOWN`；main 沒有 `.github/workflows/`。既有 Draft PR #20 CI 嘗試的 failure log 曾無法解釋，不能拿來當通過證據。 | 把可驗證的範本事實填實；不重建 PR #20 的 CI 工作，保留它為獨立未解項；Template 本次用本地等價測試及 reviewer 證據。 | Node/runtime 限制、實際 test 命令、無 lint/build/CI 與未授權部署明確列出；不得把本地測試寫成 GitHub check。**本分支已更新 facts。** |
| A-05 · P1 | Template 有 Claude／Copilot 專屬入口，但沒有載入行為矩陣；ADL 無兩種入口。不同 surface 可能忽略 `AGENTS.md`。 | Template 建立 adapter 說明；Claude `CLAUDE.md` 匯入共同檔，Copilot 專用檔只轉接；ADL 同步 adapter。 | 入口不重複政策；矩陣分列 Codex CLI/hosted、Claude、Copilot surfaces 並標出 runtime 未驗證處。**本分支已加入。** |
| A-06 · P1 | 根指令混放共通安全、Template/ADL 專屬流程與詳細操作，導致政策重複且不易判定責任。 | 根 `AGENTS.md` 收斂為必讀入口與不可破安全底線；詳細政策集中於 `docs/policies/`，`scripts/sync-shared-agent-policy.cjs` 以 Template canonical source 管理 ADL policy／adapter snapshot，預設只比對且拒絕覆蓋差異。 | Template 為共用來源；ADL snapshot bytes 相同；Adapter 只轉接，Repo-specific deployment／privacy 不被覆蓋；同步腳本驗證目標 Repo 身分並拒絕 symlink 路徑。**本分支已重整並有 CLI/文件測試。** |
| A-07 · P2 | 沒有巢狀 `AGENTS.md`。 | 不新增；當前各 Repo 是單一用途且沒有子樹規則差異。 | 若後續形成多套 build/test/安全邊界，再新增最近範圍指令並測工具支援；目前不屬缺陷。 |
| A-08 · P2 | Open PR 中有重複政策提案與互相矛盾的 Codex Cloud fallback 文字，且 Draft／缺少獨立 review 或測試結果。 | 先審查現有 PR；不重建同功能。既有 PR 不符合目前 gates 時保留未合併，後續只更新相符的既有 Issue。 | 任何 PR 需精確 head、實際 tests、獨立 review、Required Checks 查核與 merge policy；此次基線中 ADL 9 個與 Template 8 個 open PR 都沒有符合全部條件的項目。 |
| A-09 · P1 | Draft PR 缺少明確的就緒轉換判定，可能在證據齊全後仍無限停留 Draft，或被 Agent 過早升級。 | ADL 狀態機只在 acceptance、privacy scan、retrospective 均為相同 head SHA 的 PASS 時輸出單一步驟 `MARK_PR_READY_FOR_REVIEW`，下一輪重新讀取遠端狀態後才可繼續審查／合併。 | 同 SHA 全證據才輸出 promotion；缺項或舊 SHA 不輸出；升級輪不得同時 merge。純決策契約及 regression tests 已加入，ADL 164/164 與 Template 31/31 PASS；目前尚無經驗證的 GitHub 寫入 adapter，故不宣稱 live PR 已升級。 |

## 遠端及公開資料界線

本次基線盤點：ADL `main` 有 17 個 open Issues、9 個 open PRs；Template 有 6 個 Issues、8 個 open PRs。既有 PR 的獨立審查發現多個仍為 Draft、review 缺漏、CI failure root cause UNKNOWN、PR body 與實際 diff 不符或含互相矛盾的 Cloud fallback；未把這些視為可合併。Branch Protection／Rulesets API 權限限制不等於規則不存在；main `protected=false` 也不足以回答 inherited rulesets。對本輪新 PR 仍須重新查目前 Required Checks、身分 merge 權限及 repo policy。

## 官方參考

- [AGENTS.md](https://agents.md/) — Markdown 格式、推薦內容、root／巢狀範圍。
- [Codex prompting guide](https://developers.openai.com/cookbook/examples/gpt-5/codex_prompting_guide)
- [Claude Code memory files](https://code.claude.com/docs/en/memory)
- [Copilot instructions support matrix](https://docs.github.com/en/copilot/reference/custom-instructions-support)
- [Copilot repository instructions](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/add-custom-instructions/add-repository-instructions)
