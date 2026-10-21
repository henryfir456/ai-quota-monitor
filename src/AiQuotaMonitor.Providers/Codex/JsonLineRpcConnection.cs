using System.Text.Json;

namespace AiQuotaMonitor.Providers.Codex;

public sealed class JsonLineRpcConnection(TextReader reader, TextWriter writer)
{
    public async Task<string> RequestAsync(
        long id,
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Serialize(new { id, method, @params = parameters });
        await writer.WriteLineAsync(request.AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                throw new EndOfStreamException("Codex app-server closed its stdio response stream.");
            }

            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("id", out var responseId) ||
                responseId.ValueKind != JsonValueKind.Number ||
                !responseId.TryGetInt64(out var responseNumber) ||
                responseNumber != id)
            {
                continue;
            }

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
            {
                int? errorCode = error.TryGetProperty("code", out var code) && code.TryGetInt32(out var number)
                    ? number
                    : null;
                throw new CodexRpcException(errorCode);
            }

            if (!root.TryGetProperty("result", out var result))
            {
                throw new JsonException("Codex app-server response did not contain a result.");
            }

            return result.GetRawText();
        }
    }

    public async Task NotifyAsync(string method, CancellationToken cancellationToken)
    {
        var notification = JsonSerializer.Serialize(new { method, @params = new { } });
        await writer.WriteLineAsync(notification.AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
