# REPO_FACTS｜可驗證的儲存庫事實

更新日期：2026-10-20。此 Repo 是可複製範本，不代表新建 Repo 已完成產品或部署設定。

| 欄位 | 已驗證值 | 證據／限制 |
| --- | --- | --- |
| Repository | `henryfir456/agent-project-template` | 此工作分支的 GitHub remote 與 Repo metadata |
| 預設分支 | `main` | GitHub Repo metadata（2026-10-20） |
| 技術棧／工具鏈 | CommonJS Node.js scripts、JSON Schema、Markdown | `scripts/*.cjs`、`schemas/agent-loop-projects.schema.json`；無 dependency manifest |
| Node 最低版本 | UNKNOWN | 沒有 `package.json.engines`；本次可用環境 Node v24.19.0 |
| 安裝指令 | 不適用 | 範本未宣告 npm dependency |
| 測試指令 | `node --test tests/*.test.cjs` | 本次環境可執行；變更後按受影響範圍及 Repo policy 執行 |
| 設定驗證 | `node scripts/validate-agent-loop-config.cjs <config.json>` | `scripts/validate-agent-loop-config.cjs`；不要以只跑測試替代要驗證的實際設定 |
| Skill audit | `node scripts/skill-audit.cjs` | `scripts/skill-audit.cjs`；只讀本地宣告內容，不證明雲端 Skill 已載入 |
| 共用政策同步 | `node scripts/sync-shared-agent-policy.cjs <adl-root>` | 預設唯讀比對；只有經審閱後才加 `--write --replace-existing`，Repo identity 必須是 `agent-development-loop` |
| Lint / Typecheck / Build | 未配置 | Repo 內無 package scripts、lint、typecheck 或 build workflow；不能宣稱通過 |
| 版本來源檔案 | 未配置 | 範本本身沒有產品版本；使用範本建立產品後需由該 Repo 指定 |
| 繁中 UI 更新紀錄來源 | 不適用於範本本身 | 新產品依其實際 UI／release files 設定 |
| 主要程式路徑 | `scripts/`、`schemas/`、`tests/`、`.github/` | 規範與範例位於 `docs/`、`config/` |
| CI workflows | 未配置於 main | Template main 目前沒有 `.github/workflows/`；Draft PR 不算已配置或已驗證的 CI |
| 部署目標及命令 | UNKNOWN／未授權 | `docs/DEPLOYMENT.md` 明確未授權自動合併、正式部署及 migration |
| 外部依賴及敏感資料邊界 | GitHub API／Agent 介面僅為範例政策 | 不在範本內配置 Token；不得把個資、Secret 或客戶資料寫入公開紀錄 |
