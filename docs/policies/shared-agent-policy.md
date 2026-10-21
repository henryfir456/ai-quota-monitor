# 共用 Agent 開發與交付政策

版本：1.0.0。Template 是 `agent-development-loop` 與採用本範本專案的共用政策來源；ADL 以同檔名保留同步快照。跨 Repo 更新時，先改本檔，再用 `node scripts/sync-shared-agent-policy.cjs <adl-root>` 檢查；review 後以 `--write --replace-existing` 同步，再檢查 SHA-256。各 Repo 的隱私、部署、Secrets、權限與 Required Checks 局部規則仍由該 Repo 自己管理，任何同步都不得降低其安全基線。

## 規範來源與責任

- `AGENTS.md` 官方格式是一般 Markdown，沒有官方強制欄位或標準工作流程。官方建議說明專案、建置／測試、風格、安全與 PR 慣例；根目錄指令涵蓋全 Repo，巢狀檔案可加入較窄範圍規則，最近的適用檔優先。
- Codex／Claude／Copilot 是否載入某一檔案，取決於產品、版本、執行介面與設定；不能由 Markdown 格式推定。以 `docs/agent-adapters/README.md` 記錄目前支援矩陣及限制。
- 本文件的阻塞分類、重試上限、same-head 證據與合併流程是專案工程政策，不是 agents.md 官方要求。
- 政策責任由 Template 維護；ADL 負責狀態機、持久化、排程、證據檢查及執行 Gate。Repo 專屬規則優先增加約束，不得降低共同安全底線。

## 交付與證據

標準順序：`Issue → Branch → Coding → Testing → Independent Review → PR → Squash Merge → main 驗證 → Human UAT（如需要）`。每個轉移均須可稽核；PR 狀態、mergeable、草稿狀態或建立測試檔都不是通過證據。

- 開發、測試、review 與 Required Checks 證據須綁定同一個完整 head SHA。既有證據只在 SHA、測試範圍、命令及必要環境仍一致時可沿用。
- 開發者與 Reviewer 必須獨立。Reviewer 實際檢查 diff、風險、測試輸出與本次 head SHA。
- 本機／備援驗證不會變成 GitHub Required Check 通過。每一項 Required Check 須由 GitHub 即時狀態或可稽核的明確政策證據確認；查詢受限或沒找到設定時記 `UNKNOWN`，除非有當前明確證據，不能記為不存在。
- 人工 UAT、正式部署與正式資料 migration 分開判定。只有已驗證 Repo policy 明定 `AFTER_MERGE` 或 `NOT_REQUIRED` 時，pending UAT 才不阻擋合併；已驗證 `BEFORE_MERGE` 時阻擋至測試完成。時機／政策未知或未驗證時，以 `HUMAN_TEST_PENDING` 的 `BLOCKED_POLICY_UNKNOWN` 阻擋合併決策，並保留 UAT 工作；部署及 migration 永遠依各 Repo 授權，不由此流程授權。
- 只有 Repo 政策、使用者授權、GitHub 權限及所有適用 Gate 都已核實，才可 Squash Merge。未知 Required Checks 或安全 Gate 不得繞過。

### Draft PR 就緒轉換

- 若 PR 仍為 Draft，狀態機只能在 `acceptance`、`privacyScan`、`retrospective` 三項前審證據都為 `PASS` 且各自綁定目前完整 head SHA 時輸出 `MARK_PR_READY_FOR_REVIEW`。缺證據或 SHA 不符時不升級、不請求審查也不合併。
- `MARK_PR_READY_FOR_REVIEW` 是獨立且可冪等的 PR 狀態操作；該輪不得緊接著合併。下一輪必須重新讀取 GitHub 的 Draft 狀態、head SHA、review、測試和 Required Checks，再依完整 Gate 決定下一步。動作失敗或 PR 仍為 Draft 時可在證據未變的情況下重送，不得改寫證據或推定成功。
- 這是 ADL 的工程政策與決策契約，不是 agents.md 官方要求。若目前部署沒有經授權的 GitHub 寫入 adapter，決策輸出不代表 PR 已升級；保留 blocker 並繼續其他獨立工作。

## 阻塞分類與執行政策

任何 blocker 應附可安全公開的證據來源、觀測時間、Repo／Issue、影響 SHA（如適用）及安全錯誤碼。不得記錄原始憑證、敏感 log 或個資。

| 分類 | 判定所需證據 | 自動重試 | 替代驗證 | 可合併條件 | 下一步與循環上限 |
| --- | --- | --- | --- | --- | --- |
| `CODE_FAILURE` | 來源、時間、失敗命令、非零退出碼、完整 commit SHA | 同因最多 3 次；須有修正或新輸入 | 不可用不同驗證掩蓋程式失敗 | 阻塞；修正後重跑受影響 Gate | 修正程式，跑相關驗證；超限後暫停該 Issue 並輪替 |
| `TEST_FAILURE` | 來源、時間、失敗命令、非零退出碼、SHA、測試範圍 | 只有程式或環境改變才重跑；同因最多 3 次 | 僅同範圍且已核准的 runner | 阻塞，直到該 Gate 通過 | 修正測試失敗；不受影響工作繼續 |
| `CI_QUOTA_EXHAUSTED` | 明確 quota／runner 錯誤、run ID、原因碼、head SHA、失敗範圍 | 額度恢復前不重跑相同工作 | 已核准、同 SHA、同範圍的本地／備援驗證；不能代替 Required Check | 重新查核當前 Required Checks。只有適用 Required Checks 已 PASS（或有明確證據確認未設定）且其他 Gate 通過，才可能合併；未設定不是 PASS | 記錄額度證據，啟用已核准備援或輪替；同一 quota 事件最多判定一次 |
| `ENVIRONMENT_BLOCKED` | 來源、時間、錯誤碼、受阻能力 | 只在環境改變或明確 backoff 後重試；同因最多 3 次 | 僅政策核准且等效範圍的方式 | 所有 Required Checks 及其他 Gate 仍須滿足 | 保存現場、建立解阻步驟，轉做其他 Repo／Issue |
| `POLICY_BLOCKED` | 當前政策檔／設定及 revision，或精確依賴狀態 | 政策或依賴變更前不重試；最多一次確認 | 不可用備援繞過政策 | 阻塞，直到政策條件滿足 | 更新政策／依賴後重新判定；同原因不輪詢 |
| `EXTERNAL_APPROVAL_REQUIRED` | 需誰或何系統核准、核准目標 | 不自動重試 | 不適用 | 核准前阻塞 | 提出具體核准需求，其他獨立工作繼續 |
| `HUMAN_TEST_PENDING` | 測試項目、來源、時間、已驗證 Repo 設定的合併時機 | 不自動重試 | 不適用 | 已驗證 pre-merge 時阻塞至完成；已驗證 post-merge／not required 時不阻塞；政策或時機未知時以 `BLOCKED_POLICY_UNKNOWN` 阻塞 | 將人工測試留為 UAT 事項、完成指定前置測試，或補足並驗證 Repo policy |
| `UNKNOWN` | 有界調查的來源、時間、已查線索及結果 | 同 Issue／原因指紋最多 3 次調查，不無限輪詢 | 不可在分類前以替代驗證消除 blocker | 阻塞該 Issue 的合併 | 第 3 次記錄 `UNKNOWN_EXHAUSTED`、保留待解原因並輪替；新證據或 Issue revision 變更可建立新調查序列 |

### Required Checks 與 quota 判定

1. 先確認失敗是否有明確 quota／runner 證據；一般 CI 紅燈屬 `TEST_FAILURE`，未能讀取 workflow log 屬 `UNKNOWN` 或 `ENVIRONMENT_BLOCKED`，不可推成 quota。
2. 重新查明目標分支當前 Branch Protection、套用的 Rulesets 及實際 Required Checks。`protected=false`、403、空 API 結果或沒有找到設定，單獨都不足以證明 Required Checks 不存在。
3. quota 阻塞時禁止重跑同一個 Actions 工作直到額度／runner 改變。若已有核准備援，對同一 SHA、同等測試範圍執行並保存命令、runtime、結果及 artifact；Required Check 仍須由 GitHub 或有效政策判定。
4. 任何必需的 GitHub Check 未通過或狀態未知，仍禁止合併。不能因為是文件變更、人工測試待做或 quota 耗盡而忽略真正適用的 Required Check。

### 復原與公平輪替

- blocker 只隔離受影響的 Issue／Repo。控制器保存其狀態、錯誤指紋、SHA 與有界調查計數，再挑選無依賴的下一項工作。
- 重啟必須讀回未完成狀態與已寫入證據；重複執行使用 idempotency key，不得重複 claim、派工、PR 或 merge。
- 配額、外部核准、人工 UAT 與 `UNKNOWN` 用明確的恢復條件；條件未改變時不可重跑。若沒有可執行工作，輸出 `BLOCKED`／責任端／最低解阻動作，不得假稱已完成。
