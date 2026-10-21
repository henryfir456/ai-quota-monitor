# 2026-10-20 多 Repo Agent Loop 設定契約回顧

- Repository／任務：`henryfir456/agent-project-template`，MVP Task 1 設定契約
- 分支／PR／Commit SHA：`mvp/agent-loop-project-config`／尚未建立 PR／實作 commit `be3361a42608998c0683f9aec51f782ab6ccd5c5`
- 本輪範圍及變更：新增 schema version 1、三 Repo scan-only 範例、無 dependency 的 CJS 驗證器及拒絕未知權限／額度／路徑的測試。
- 結果：DONE（僅代表本設定契約切片；整體 MVP 尚未完成）

## 遇到的阻礙

| 現象與發生階段 | 分類 | 證據 | 嘗試與結果 | 根因及信心 |
| --- | --- | --- | --- | --- |
| 既有測試需 spawn 子 Node，標準工作區沙箱回傳 EPERM | 環境／權限 | `node --test tests/skill-audit.test.cjs` 在預設沙箱 exit 1；子程序錯誤為 `spawnSync .../node EPERM` | 在授權的本機測試程序權限下執行相同測試，17/17 PASS、exit 0；未變更沙箱策略或產品程式 | 已驗證：預設沙箱禁止該子程序；升權本機測試可正常執行 |
| 新增路徑負向測試發現 Windows 絕對路徑未被拒絕 | 程式／安全 | `node tests/agent-loop-config.test.cjs` 第一次為 7/8 PASS，`C:\\data\\state.sqlite` 被接受 | 增加 POSIX／Windows 絕對路徑、NUL 與 parent traversal 拒絕，測試後 8/8 PASS | 已驗證：原驗證只判斷 POSIX `/` 開頭及 `..` segment |

## 個資發布檢查

- PII_SCAN：NOT VERIFIED；目前無對外發布，本地測試皆為合成設定。建立 PR 前仍須針對最終 diff 獨立執行並覆核。
- Issue／PR／commit／測試輸出／截圖／評論均使用合成資料，確認者：Developer（本地工作樹）

## 驗證

- `node tests/agent-loop-config.test.cjs`：Node v24.19.0，8/8 PASS，exit 0。
- `node --test tests/agent-loop-config.test.cjs`：PASS，exit 0。
- `node --test tests/skill-audit.test.cjs`：在授權的本機測試程序權限下 17/17 PASS，exit 0；預設 sandbox 結果另列上述阻礙。
- `node --check scripts/validate-agent-loop-config.cjs`、`node --check tests/agent-loop-config.test.cjs`：PASS，exit 0。
- Template 沒有 `package.json`；依 AGENTS 與既有測試直接執行 Node 測試。未執行 CI／Actions。
- 未驗證項目：最終 PII_SCAN、獨立 review、PR Required Checks 與合併政策需在交付前重新查核。

## 建議改善與遺留問題

| 建議／待辦 | 優先級 | 類型 | 負責者或追蹤連結 | 驗收條件 |
| --- | --- | --- | --- | --- |
| 衍生範本時填妥 `docs/REPO_FACTS.md` 的工具鏈與測試命令 | P2 | 規範改進 | 範本使用者 | 由目標 Repo 實際檔案及命令佐證，不保留未核對猜測 |

## 下次預防與可重用知識

- 可重現觸發條件：Windows drive／UNC 路徑或 NUL 字元進入跨平台控制器 state path。
- 建議新增的前置檢查／測試／守門條件：設定採嚴格 key allowlist、明確能力布林及有限安全預算；保留跨平台路徑負向測試。
- 是否需要更新 AGENTS.md、docs/standards 或 CI：否；範例與離線驗證器不改變既有範本 CI 或部署政策。

## Task 7 最終交付核對（2026-10-20）

- Template main：`4e87a49e611b5b658eb06578e8a39e338ad722c9`；最新遠端盤點為 6 個 open Issues、8 個 open PRs，沒有對 DailyCheckIn_Android 或 bento-order-app 建立產品變更。
- 最終命令 `node --test tests/*.test.cjs`：Node v24.19.0，30/30 PASS，exit 0；子程序測試在核准的本機測試程序權限下執行。
- 設定 schema parity：與 ADL `schemas/agent-loop-projects.schema.json` 使用 `cmp` 驗證相同；SHA-256 為 `6ae2a83a44bd7830701a73006a7c8374096c21f9debfeaef780b1f18e4c3f347`。JSON Schema 與 Template runtime validator 都拒絕 POSIX、Windows drive/root-relative 絕對路徑、parent traversal 與 NUL。
- 最終 staged diff PII_SCAN：PASS（沒有 Secret/private-key/email pattern match）；獨立分支審閱仍須在 PR 前對精確 branch head 執行。本檔只含合成 Repo 名稱、schema SHA 和本機測試結果。
- GitHub main 最新查核仍顯示 `protected=false`；Branch Protection endpoint 回 `403 Resource not accessible by integration`，Rulesets endpoint 回方案限制 `403 Upgrade to GitHub Pro...`。AGENTS.md 保存的維護者「無 Rulesets」確認日期為 2026-10-19，並非本日新確認；因此本日 Required Checks/merge policy 視作 UNKNOWN，不宣稱 PASS，也不因此啟用 Auto-merge。
- 無 live Template API write、Actions rerun、PR merge 或 Agent dispatch。此 Repo 提供設定契約，不提供無人值守 Agent runtime。

## 2026-10-20 AGENTS 相容性與共用閉環政策增補

- 本輪範圍：Template root `AGENTS.md` 收斂為入口；原詳細政策移至 `docs/policies/agent-workflow.md`；新增 Template canonical `docs/policies/shared-agent-policy.md`、工具 surface 矩陣、官方／產品／自訂政策分層及雙 Repo 問題清單。
- Codex、Claude、Copilot runtime 載入均未在本環境實測；文件只記錄官方產品文件所載行為與本 Repo 入口，不宣稱所有 IDE／cloud surface 通過。
- `docs/REPO_FACTS.md` 的 UNKNOWN 欄位改為已檢查事實或明確未配置狀態；main 無 `.github/workflows/`，不將既有 Draft CI PR 當成本次 CI PASS。
- Template `node --test tests/*.test.cjs`：Node v24.19.0，30/30 PASS，exit 0。新增測試確認 8 類 policy 條目、UNKNOWN 上限、Required Checks 語意、adapter 不複製完整政策，以及同步腳本目標 Repo／覆寫確認／symlink 守門。
- 共用政策在 ADL 與 Template worktree 用 `cmp` byte-identical；SHA-256 `dbf976df96d61ab99ae8d5f67381ff9f0f0f6a39b9b8fac9d93c0f2084be4ec2`。Template sync CLI 預設唯讀檢查並限 `agent-development-loop` Repo；schema parity SHA-256 仍是 `6ae2a83a44bd7830701a73006a7c8374096c21f9debfeaef780b1f18e4c3f347`。
- 本次新增文件沒有修改 Template 部署、auto-merge、Secrets 或權限政策。PR、Required Checks 當前配置與手動 merge 權限仍需在建立交付 PR 後重新查核；本回顧不宣稱已合併。
