# 依賴清單

## 發行程式

- Target: .NET 8 WPF + Windows Forms system tray, self-contained `win-x64` portable publish。
- 應用專案目前沒有第三方 NuGet `PackageReference`；Core、Providers 是本 repository 專案。
- 發行檔包含 .NET 8 self-contained Windows runtime。精確 runtime/package 版本與 SPDX SBOM 隨每個 Release 由 Windows runner 從 publish dependency manifest 產生。

## 建置／測試

| 套件 | 版本 | 用途 |
| --- | --- | --- |
| Microsoft.NET.Test.Sdk | 17.12.0 | 測試 host |
| xunit | 2.9.3 | 單元測試 |
| xunit.runner.visualstudio | 3.0.1 | Windows test runner |

NuGet 版本固定於 `tests/AiQuotaMonitor.Tests/AiQuotaMonitor.Tests.csproj`；restore 啟用 vulnerability audit 並將 NU1901～NU1904 視為錯誤。

## GitHub Actions

| Action | Pin | 用途 |
| --- | --- | --- |
| actions/checkout | `11d5960a326750d5838078e36cf38b85af677262` | Checkout |
| actions/setup-node | `49933ea5288caeca8642d1e84afbd3f7d6820020` | Node 20 repository governance tests |
| actions/setup-dotnet | `67a3573c9a986a3f9c594539f4ab511d57bb3ce9` | .NET SDK |
| actions/upload-artifact | `ea165f8d65b6e75b540449e92b4886f43607fa02` | TRX artifact |
| gitleaks/gitleaks-action | `ff98106e4c7b2bc287b24eaf42907196329070c7` | Secret scan |
| softprops/action-gh-release | `3bb12739c298aeb8a4eeaf626c5b8d85266b0e65` | v0 prerelease |

Action refs were resolved from their public GitHub tag endpoints on 2026-10-21; workflows use immutable commit SHAs instead of moving tags.
