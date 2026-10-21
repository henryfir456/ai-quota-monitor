using System.Text.Json;

namespace AiQuotaMonitor.Providers.Codex;

public static class CodexAccountStatusParser
{
    public static bool IsAuthenticated(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Codex account status response is not an object.");
        }

        if (root.TryGetProperty("result", out var envelopeResult))
        {
            root = envelopeResult;
        }

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("account", out var account))
        {
            throw new JsonException("Codex account status response did not include account state.");
        }

        if (account.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        if (account.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Codex account state has an unsupported shape.");
        }

        return true;
    }
}
