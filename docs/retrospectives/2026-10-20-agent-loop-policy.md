# 2026-10-20 共用 Agent 政策與相容性回顧

- Repository／任務：`henryfir456/agent-project-template`；與 ADL controller-first MVP 同步的政策、adapter 與官方 AGENTS.md 相容性審計。
- 分支／PR／Commit SHA：`mvp/agent-loop-project-config`／[#21](https://github.com/henryfir456/agent-project-template/pull/21)／政策與文件修正提交 `8581917c44e3fb5d480c62caa96408ffd76f1303`；本回顧檔隨後加入同一分支。
- 本輪範圍及變更：縮小 root `AGENTS.md`；以 `docs/policies/shared-agent-policy.md` 為共用來源；分開記錄 Codex／Claude／Copilot 各 surface 實際能力；補 blockers、相同 SHA Gate、Draft 分輪升級、UAT policy unknown、Issue/PR 交付規則；同步 ADL mirror；修正搬移後的 policy 相對路徑並加入解析測試。
- 結果：PARTIAL。共用政策與文件測試完成；無人值守 Agent 派工、各工具 runtime 載入及正式 GitHub merge 未驗證。

## 遇到的阻礙

| 現象與發生階段 | 分類 | 證據 | 嘗試與結果 | 根因及信心 |
| --- | --- | --- | --- | --- |
| PR #21 Draft | 規範 | PR #21 latest code head `8dfa657edf01d8aa4150eea36d63e58752ad3fba` | 獨立 Reviewer 已完成 exact-head 對抗式審查，無可行動 P0/P1/P2；技術審查建議 Ready。這是 Agent 審查證據，不代表 GitHub check 或 merge 已完成 | 先前 policy link 修正已驗證；PR state transition／merge 仍需另行查核 |
| GitHub Branch Protection／Rulesets 查詢受限 | 權限 | main branch API 回報 `protected=false`、`protection.enabled=false`、Required Status Checks enforcement `off` 且 contexts/checks 空；Rulesets API 403 | main SHA 仍是 `4e87a49e611b5b658eb06578e8a39e338ad722c9`，與 main 維護者在 2026-10-19 確認無 Branch Protection／Rulesets 的快照相同。保護設定 Required Checks 分類 `NOT CONFIGURED`；Rulesets API 限制獨立保留 | 403 不是無規則的證據；本次結論有明確 main branch API 設定及未變動 main 快照支持，內部 Gate 仍強制 |
| Codex Cloud／ChatGPT Work／Claude／Copilot runtime dispatch 未驗證 | 能力 | 本執行環境沒有可呼叫的無人值守派工＋結果回寫介面 | 不啟動外部 Agent；在 PR #21／Issue #13 明確列出能力缺口 | `UNKNOWN`；不得由產品訂閱推定 API 授權 |

## 個資發布檢查

- PII_SCAN：PASS。Template staged 檔案內容的高信心 token／私鑰／email／電話模式掃描為 0 match；不代表語意姓名識別。
- Issue／PR／commit／測試輸出均使用合成資料；沒有寫入秘密或使用者個資。

## 驗證

- `node --test tests/*.test.cjs`，Node v24.19.0，exit 0，32/32 PASS；涵蓋設定 schema、安全政策與 adapter、同步工具身份／symlink 拒絕、文件相對連結。
- Template ↔ ADL 共用 policy／adapter sync：PASS，差異為空；設定 JSON Schema byte parity：PASS。
- Template main 沒有 GitHub Actions workflow；保護設定 Required Checks 為 `NOT CONFIGURED`（branch endpoint 明確無啟用 protection/status contexts，且 main 快照與維護者確認一致）。PR #21 測試 32/32、共用政策同步與 Schema parity PASS；獨立 Reviewer 對 `8dfa657edf01d8aa4150eea36d63e58752ad3fba` 無可行動 finding，建議 Ready。記錄時 PR 仍 Draft，尚未 merge。
- 未驗證項目：Claude/Copilot/Codex 各 runtime 的實際 instruction discovery、無人值守 dispatch/result callback、跨 repo 真實 E2E、正式部署與資料 migration。

## 建議改善與遺留問題

| 建議／待辦 | 優先級 | 類型 | 負責者或追蹤連結 | 驗收條件 |
| --- | --- | --- | --- | --- |
| 確認可授權的 Agent dispatch＋結果回寫介面，否則保留 fail-closed | P1 | 後續 Issue | [Issue #13](https://github.com/henryfir456/agent-project-template/issues/13) | 具可審計授權、合成 Repo PoC、結果驗證及停止條件 |
| 完成 PR #21 Ready／squash 與 main 驗證 | P1 | 交付步驟 | [PR #21](https://github.com/henryfir456/agent-project-template/pull/21) | 更新 PR 至最終 head，重讀 Draft／mergeability／reviews／權限；依使用者授權和已驗證必要 Gate 執行或記錄精確阻塞 |
| 官方規範與工具支援差異保持分開記錄 | P2 | 規範改進 | `docs/AGENTS_COMPATIBILITY_AUDIT.md` | 來源、surface、驗證狀態均清楚，runtime 未測一律 NOT VERIFIED |

## 下次預防與可重用知識

- 可重現觸發條件：文件由 `docs/` 移入 `docs/policies/`，`../docs/...` 會錯指到 `docs/docs/`。
- 前置檢查：對政策文件 Markdown local links 做存在性測試，並掃描殘留的 `../docs/`。
- 本輪已新增 policy link regression test，並在 ADL 加上同範圍測試。
- 是否需要更新 AGENTS.md、docs/standards 或 CI：共用政策已同步；Template 沒有 CI workflow，本輪不新增 trigger／權限。runtime capability 仍待獨立 PoC，不把文件描述升格成已驗證能力。
