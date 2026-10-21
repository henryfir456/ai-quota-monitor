using AiQuotaMonitor.Providers.Codex;

namespace AiQuotaMonitor.Tests;

public sealed class CodexAccountStatusParserTests
{
    [Fact]
    public void NullAccountIsReportedAsNotLoggedInWithoutReturningAccountFields()
    {
        const string response = """{"account":null,"requiresOpenaiAuth":true}""";

        var authenticated = CodexAccountStatusParser.IsAuthenticated(response);

        Assert.False(authenticated);
    }

    [Fact]
    public void AuthenticatedAccountIsDetectedWithoutExposingEmail()
    {
        const string response = """{"account":{"type":"chatgpt","email":"synthetic@example.invalid","planType":"plus"},"requiresOpenaiAuth":true}""";

        var authenticated = CodexAccountStatusParser.IsAuthenticated(response);

        Assert.True(authenticated);
    }

    [Fact]
    public void UnexpectedAccountSchemaFailsClosed()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => CodexAccountStatusParser.IsAuthenticated("{\"requiresOpenaiAuth\":true}"));
    }
}
