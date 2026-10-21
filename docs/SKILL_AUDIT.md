# Balanced Agent Skill Audit

This repository uses a **zero-extra-model-call** static audit. Run from repository root:

```bash
node scripts/skill-audit.cjs --out reports/skill-audit
node scripts/skill-audit.cjs --baseline reports/prior-skill-audit.json --out reports/skill-audit
```

The scanner uses only Node.js built-ins; it makes no network/model calls, never automatically launches a reviewer, and does not change authorization, privacy, CI, merge or deployment gates. Its YAML parser intentionally accepts only literal `path`, `skill_path`, or `file` entries; unsupported schemas are **UNVERIFIED**, not PASS. Missing `agent.yaml` is **NOT_APPLICABLE**. If manifest data refers to a canonical external skill not present locally, report an unresolved reference; never substitute an unpinned/global Skill.

Artifacts: `reports/skill-audit.json` and `.md`. Avoid committing reports that contain sensitive paths. This code does not collect source content, personal data, raw prompts or secrets; outputs contain relative skill paths and hashes. Findings triggering conditional review are **suggestions** routed into the existing Issue → PR → independent review → retrospective process, not automatic AI executions. Keep safety checks mandatory even when audit is deferred.

`additionalModelCalls: 0` describes **this scanner only**. Actual Codex tokens/credits and outcome impact remain `UNKNOWN`/`UNVERIFIED` unless separately measured with provider-supported counters, a clearly scoped before/after baseline, and reproducible task evidence. Never infer usage from a self-reported Agent statement. A/B evaluation is an opt-in manual research task and must not run on every PR.

Freshness: re-run when a Skill/manifest/architecture changes. Broken links, runtime/CLI deprecations, and semantic contradictions need independent verification; static existence/hashes cannot certify freshness. Maintain a dated human-reviewed follow-up when a check is not automated.

## Path and baseline safety / 路徑與 Baseline 安全

Manifest and Baseline inputs must resolve within the repository; external symlinks fail closed. Unreadable Manifest or Skill inputs, including directories, are reported as **UNVERIFIED**. Baseline comparisons include added, changed, and removed skills. Existing output ancestors are resolved before directory creation, and generated JSON/Markdown files are opened without following final-component symlinks, so output links cannot redirect writes outside the repository. A malformed Baseline `skills` array or entry is reported as **UNVERIFIED** without an uncaught exception.

Manifest 與 Baseline 輸入必須解析至 Repository 範圍內；指向外部的 symlink 會 fail closed。無法讀取的 Manifest 或 Skill（含目錄）標記為 **UNVERIFIED**。Baseline 比對會涵蓋新增、修改及移除的 Skill。建立輸出目錄前會解析既有路徑，並以不跟隨最終檔案 symlink 的方式寫入 JSON／Markdown，避免報告被導向 Repository 外。Baseline 的 `skills` 陣列或項目格式錯誤時標記為 **UNVERIFIED**，不讓檢查器發生未處理例外。
