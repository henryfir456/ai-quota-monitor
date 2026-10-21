namespace AiQuotaMonitor.Providers.Codex;

public sealed class CodexRpcException(int? errorCode)
    : Exception("Codex app-server returned an RPC error.")
{
    public int? ErrorCode { get; } = errorCode;
}
