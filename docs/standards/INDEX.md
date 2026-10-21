# 語言與平台工程規範索引

所有 Agent 由根目錄 AGENTS.md 進入此索引。先讀 docs/REPO_FACTS.md，確認實際工具鏈與目標環境；只套用任務涉及的語言及平台。未列出語言時，不得直接宣稱無須驗證，先盤點目標 Runtime 與官方相容性，再建立最小規範。

| 變更類型 | 必讀規範 | 最低證據 |
| --- | --- | --- |
| .ps1/.psm1/.psd1 | powershell.md、windows.md | 目標 PowerShell 解析；Windows PS5.1 檔案需 BOM |
| .bat/.cmd | windows.md | CRLF 與 cmd.exe 實際啟動 |
| .ts/.tsx/.js/.jsx | typescript.md | 實際專案的 lint、typecheck、test、build |
| .py/.pyi | python.md | 目標 Python 版本解析與測試 |
| .kt/.kts/.java | kotlin-java.md | Gradle/JDK/Android 版本匹配 |
| .cbl/.cob/.cpy | cobol-aix.md | 編譯器、字元集、固定／自由格式 |
| Workers/D1/R2 | cloudflare.md 加原始語言規範 | 本地測試、migration 與部署分離 |

混合語言修改必須合併所有相關規範。README 是人類操作說明；Skills 是操作程序，不等同強制的格式、驗證規範。缺乏執行環境時標示 NOT VERIFIED，且不得冒稱真機或正式環境通過。
