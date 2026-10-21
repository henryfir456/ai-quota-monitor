using AiQuotaMonitor.Providers.Codex;

namespace AiQuotaMonitor.Tests;

public sealed class CodexAppServerClientTests
{
    [Fact]
    public async Task InitializesExperimentalCapabilityAndReadsAccountWithoutRefreshingToken()
    {
        using var reader = new StringReader("""
            {"id":0,"result":{}}
            {"id":1,"result":{"account":{"type":"chatgpt","email":"synthetic@example.invalid"},"requiresOpenaiAuth":true}}
            {"id":2,"result":{"rateLimitsByLimitId":{"codex":{"primary":{"usedPercent":35,"windowDurationMins":300}}}}}
            """);
        using var writer = new StringWriter();
        var connection = new JsonLineRpcConnection(reader, writer);

        var response = await CodexAppServerClient.ReadRateLimitsFromRpcAsync(connection, CancellationToken.None);
        var sentRequests = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("\"capabilities\":{\"experimentalApi\":true}", sentRequests[0], StringComparison.Ordinal);
        Assert.Contains("\"method\":\"account/read\"", sentRequests[1], StringComparison.Ordinal);
        Assert.Contains("\"refreshToken\":false", sentRequests[1], StringComparison.Ordinal);
        Assert.Contains("\"method\":\"account/rateLimits/read\"", sentRequests[2], StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic@example.invalid", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingAccountStopsBeforeQuotaRequestAndUsesSanitizedException()
    {
        using var reader = new StringReader("""
            {"id":0,"result":{}}
            {"id":1,"result":{"account":null,"requiresOpenaiAuth":true}}
            """);
        using var writer = new StringWriter();
        var connection = new JsonLineRpcConnection(reader, writer);

        var exception = await Assert.ThrowsAsync<CodexNotLoggedInException>(() =>
            CodexAppServerClient.ReadRateLimitsFromRpcAsync(connection, CancellationToken.None));

        Assert.Equal(2, writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.DoesNotContain("Token", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OlderServerWithoutAccountReadContinuesToTheRateLimitMethod()
    {
        using var reader = new StringReader("""
            {"id":0,"result":{}}
            {"id":1,"error":{"code":-32601,"message":"synthetic private server message"}}
            {"id":2,"result":{"rateLimitsByLimitId":{"codex":{"primary":{"usedPercent":35,"windowDurationMins":300}}}}}
            """);
        using var writer = new StringWriter();
        var connection = new JsonLineRpcConnection(reader, writer);

        var response = await CodexAppServerClient.ReadRateLimitsFromRpcAsync(connection, CancellationToken.None);

        Assert.Contains("rateLimitsByLimitId", response, StringComparison.Ordinal);
        Assert.Contains("account/rateLimits/read", writer.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic private server message", writer.ToString(), StringComparison.Ordinal);
    }
}
