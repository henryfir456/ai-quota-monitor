using System.IO.Pipes;
using System.Text;

namespace AiQuotaMonitor.Providers.Claude;

public sealed class ClaudeStatuslinePipeHost(ClaudeStatuslineProvider provider)
{
    public const string PipeName = "AiQuotaMonitor.ClaudeStatusline.v1";
    private const int MaximumPayloadLength = 16 * 1024;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                using var reader = new StreamReader(
                    pipe,
                    new UTF8Encoding(false),
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is not null && line.Length <= MaximumPayloadLength)
                {
                    provider.Accept(line);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (IOException)
            {
                // The local statusline bridge is optional; never log its input or errors.
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
