# 更新紀錄

遵循 Keep a Changelog 的精神；實際版號需與專案版本來源一致。

## [Unreleased]
### 新增
- 收斂根 `AGENTS.md` 為政策入口，將詳細規則放入 `docs/policies/`；新增 agents.md 官方格式與 Codex／Claude／Copilot surface 相容性審查，區分官方慣例、工具能力和 Repo 自訂政策。
- 新增 Template canonical 共用 blocker policy 與唯讀預設同步腳本，明列八類阻塞的證據、重試、替代驗證、合併條件及輪替方式；補實 REPO_FACTS 並新增政策／adapter／同步腳本回歸測試。
- 建立繁體中文 AI 開發範本的治理與文件骨架。
- 加入版本化多 Repo Agent Loop 設定範例、JSON Schema 與離線 fail-closed 驗證器；三個初始 Repo 僅啟用安全掃描。
- 重新驗證 Template 設定契約與 ADL runtime schema 完全一致；schema／policy/docs 測試 30/30 通過，仍未開啟任何寫入 capability。
- Schema pattern 現在也拒絕 POSIX／Windows drive 和 root-relative 絕對路徑、parent traversal 與 NUL state path；新增契約回歸測試，Template 全套測試 31/31 通過。
- 共用交付政策加入 Draft PR 的 same-head acceptance、privacy scan、retrospective 前審證據；ADL 只在證據齊全時輸出獨立 Ready 動作，下一輪重讀遠端狀態才可繼續。

### 修正
- Skill Audit 拒絕透過外部符號連結讀取 Manifest／Baseline 或寫入報告；無法讀取的 Manifest／Skill 與錯誤 Baseline 結構回報 UNVERIFIED，Baseline 也會偵測已移除的 Skill，並增加對應回歸案例。

> 範本不代表任何應用已發布。建立新專案後依 `docs/REPO_FACTS.md` 配置版本／繁中 UI changelog 的同步檢查。
