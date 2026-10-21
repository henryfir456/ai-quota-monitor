# DEVELOPMENT｜開發流程

1. Intake：確認需求、成功條件與不可做的事。
2. Preflight：識別根目錄、分支、HEAD、工作樹、使用者既有修改。
3. Context：讀取 AGENTS、Facts、State、Architecture、Decisions 和需要的程式。
4. Plan freeze：大改動先核對範圍、依賴、驗證、風險及回滾方案。
5. Implementation：最小修改，保存既有程式與用戶資料；避免無關重構。
6. Verification：執行專案真正提供的自動測試，不能憑想像給 PASS。
7. Review：獨立 reviewer 審查同一 head SHA；變動後重新確認審閱效力。
8. Handoff：記錄成果、未驗證項、剩餘 Issue 及下一步。

有固定且經驗證的多次重複流程，才考慮移到 Skills；不要一個步驟一個 Skill。
