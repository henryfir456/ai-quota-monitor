using AiQuotaMonitor.Providers.Codex;

namespace AiQuotaMonitor.Tests;

public sealed class CodexRpcConnectionTests
{
    [Fact]
    public async Task RequestIgnoresNotificationsAndUnrelatedResponseIds()
    {
        using var reader = new StringReader("""
            {"method":"account/rateLimits/updated","params":{}}
            {"id":99,"result":{}}
            {"id":7,"result":{"ok":true}}
            """);
        using var writer = new StringWriter();
        var connection = new JsonLineRpcConnection(reader, writer);

        var response = await connection.RequestAsync(7, "account/rateLimits/read", new { }, CancellationToken.None);

        Assert.Equal("{\"ok\":true}", response);
        Assert.Contains("\"id\":7", writer.ToString());
        Assert.Contains("\"method\":\"account/rateLimits/read\"", writer.ToString());
    }

    [Fact]
    public async Task RpcErrorExposesOnlyCodeAndDoesNotLeakServerMessage()
    {
        using var reader = new StringReader("""{"id":2,"error":{"code":-32001,"message":"synthetic-server-text"}}""");
        using var writer = new StringWriter();
        var connection = new JsonLineRpcConnection(reader, writer);

        var error = await Assert.ThrowsAsync<CodexRpcException>(() =>
            connection.RequestAsync(2, "account/rateLimits/read", new { }, CancellationToken.None));

        Assert.Equal(-32001, error.ErrorCode);
        Assert.False(error.ToString().Contains("synthetic-server-text", StringComparison.Ordinal));
    }
}
