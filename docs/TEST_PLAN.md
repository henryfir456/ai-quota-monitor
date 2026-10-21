# TEST_PLAN｜驗證計畫

| 領域 | 驗證方法 | 狀態 | 證據 |
| --- | --- | --- | --- |
| .NET restore/NuGet audit | Windows runner · `dotnet restore AiQuotaMonitor.sln`; NU1901–NU1904 errors | NOT RUN | PR Actions pending |
| Repository governance tests | Ubuntu runner · `node --test tests/*.test.cjs` | BLOCKED locally; CI pending | Local run hit sandbox `EPERM` on nested Node processes; CI job added |
| Release build | Windows runner · `dotnet build AiQuotaMonitor.sln -c Release --no-restore` | NOT RUN | PR Actions pending |
| xUnit | Windows runner · `dotnet test AiQuotaMonitor.sln -c Release --no-build` | NOT RUN | TRX artifact pending |
| Codex schema / RPC | synthetic fixture parser, timeout, unsupported, environment allowlist and error redaction tests | NOT RUN | Windows xUnit pending |
| Claude parser / pipe | official field synthetic fixture; Windows PS 5.1 parser; CurrentUserOnly bridge integration with synthetic session markers | NOT RUN | Windows CI pending |
| Settings | schema fallback, atomic write, safe default/range and no credential fields | NOT RUN | Windows xUnit pending |
| Secret scan | Gitleaks action pinned commit, full history | NOT RUN | PR Actions pending |
| WPF/XAML / WinForms / self-contained publish | Windows Desktop SDK build and v0.1.0 tag publish | NOT RUN | Release workflow pending |
| Windows 11 x64 GUI / process UAT | `docs/UAT_WINDOWS11.md` | HUMAN_TEST_PENDING | no Windows 11 device available in current environment |
| Provider real-client matrix | real Codex CLI/Desktop version and user-configured Claude statusline | NOT VERIFIED | no target Windows application versions established |

本機已執行 Python XML parse（XAML/csproj/manifest）與 YAML parse（GitHub Actions workflow）；這些只屬靜態語法檢查，不能替代編譯、測試、Windows PowerShell 5.1、Windows UI 或 UAT。

Result vocabulary: `PASS`、`FAIL`、`NOT RUN`、`NOT VERIFIED`、`HUMAN_TEST_PENDING`。所有測試 artifact、命令、runtime、退出碼及 SHA 在執行後填入 `docs/TEST_REPORT.md`；未執行項目不推定通過。
