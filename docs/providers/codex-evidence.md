# Codex 額度來源證據

觀測日期：2026-10-21

## 已核實來源

- Codex 官方 app-server 文件：<https://developers.openai.com/codex/app-server>。文件說明 `codex app-server` 預設使用 stdio，透過 newline-delimited JSON 傳輸；連線先送 `initialize`，再送 `initialized`。讀取額度的方法是 `account/rateLimits/read`，通知為 `account/rateLimits/updated`。官方文件同時標示 app-server 為實驗性介面，介面可能變更。
- 官方 protocol 原始碼，觀測於 `openai/codex` commit [`d571eaf1f61d0d93a797506335a99a9950ffe37b`](https://github.com/openai/codex/commit/d571eaf1f61d0d93a797506335a99a9950ffe37b)：
  - [`protocol/common.rs`](https://github.com/openai/codex/blob/d571eaf1f61d0d93a797506335a99a9950ffe37b/codex-rs/app-server-protocol/src/protocol/common.rs) 宣告 `GetAccountRateLimits => "account/rateLimits/read"`，response 為 `GetAccountRateLimitsResponse`。
  - [`protocol/v2/account.rs`](https://github.com/openai/codex/blob/d571eaf1f61d0d93a797506335a99a9950ffe37b/codex-rs/app-server-protocol/src/protocol/v2/account.rs) 與產生的 [RateLimitWindow schema](https://github.com/openai/codex/blob/d571eaf1f61d0d93a797506335a99a9950ffe37b/codex-rs/app-server-protocol/schema/typescript/v2/RateLimitWindow.ts) 定義 `usedPercent`、`windowDurationMins`、`resetsAt`。官方型別註解將 `resetsAt` 定義為 Unix epoch 秒。
  - `GetAccountRateLimitsResponse` 提供舊版單 bucket `rateLimits` 與多 bucket `rateLimitsByLimitId`；多 bucket 以 `codex` 識別。只使用 Codex bucket 或明確的舊版單 bucket。
- 參考實作 `henryfir456/codex-usage-companion` 的 [CodexAppServerSession.cs](https://github.com/henryfir456/codex-usage-companion/blob/7aa4643faeecda977d2bd59fca1a96fa3e85dd7b/src/CodexUsageCompanion/RateLimits/CodexAppServerSession.cs)、[RateLimitParser.cs](https://github.com/henryfir456/codex-usage-companion/blob/7aa4643faeecda977d2bd59fca1a96fa3e85dd7b/src/CodexUsageCompanion/RateLimits/RateLimitParser.cs) 示範 JSONL 初始化與唯讀查詢。此專案只參考協定與映射概念，不執行其 EXE，也不複製其非官方 fallback 推測。

## 版本與支援狀態

- 本次 Linux 開發環境不是使用者的 Windows 11，沒有可確認的 Codex Desktop/CLI 安裝版本。Target CLI/Desktop version：`UNKNOWN`；未執行 `--version`、登入或 app-server。
- 上游 protocol commit 只能證明該 commit 的 schema，不等同已驗證某個已發行 CLI 版本。程式會以 runtime RPC 結果判斷；未知方法、格式變更或未支援版本 fail closed。
- 官方文件稱 app-server 為實驗性介面。Windows 真實 CLI 整合驗收仍列入 Issue #7 UAT。

## 欄位映射

`primary`／`secondary` 不代表固定時間窗；只依來源回報的 `windowDurationMins` 映射：`300` → 5 小時，`10080` → 每週。每個有效 `usedPercent` 轉為 `RemainingPercent = 100 - usedPercent`。weekly-only 回覆只填每週欄位，5 小時明確 `UNSUPPORTED`；沒有 duration、percent 或 reset 不補值、不按 primary/secondary 順序猜測。`resetsAt` 依官方型別以 Unix 秒轉 UTC；毫秒形狀、溢位或非法時間不猜測轉換，reset 顯示 N/A。

## 合成回覆

以下資料完全虛構，測試 fixture 不含帳號、Token 或真實 session：

```json
{
  "id": 1,
  "result": {
    "rateLimitsByLimitId": {
      "codex": {
        "primary": { "usedPercent": 35, "windowDurationMins": 300, "resetsAt": 1800000000 },
        "secondary": { "usedPercent": 62, "windowDurationMins": 10080, "resetsAt": 1800600000 }
      }
    },
    "rateLimits": {}
  }
}
```

## 執行與錯誤邊界

- 只直接啟動已找到的 `codex.exe app-server`，使用 stdio JSONL；不經 shell，不送 `login`、thread/turn、model 或 inference 方法。
- 每次刷新啟動一個短生命週期 app-server child process，讀取一個 account rate-limit response 後終止；不留下常駐 child process。
- Monitor 不開啟 `auth.json`，不輸出 app-server stderr、錯誤內容、raw response 或 account metadata。只抽取 percent/reset 欄位；敏感 API key 類環境變數不傳給 child process。Codex app-server 可能使用 Codex 自己管理的登入狀態；該流程由 Codex 控制，不由 Monitor 實作。
- CLI 不存在、未登入或帳號資料不可用不會觸發登入；回報 UNKNOWN/UNSUPPORTED。RPC method 不存在回報 UNSUPPORTED，timeout/cancel/error 由單來源 timeout 與錯誤狀態處理。
- 唯一資料用途是本機額度卡片與低額度提醒；app-server 依 Codex 自己的網路設定取回 ChatGPT 額度。Monitor 不另行呼叫 OpenAI endpoint。
