using System.Threading.Channels;
using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Interfaces;

namespace PriorAuthorization.Infrastructure.Queue;

/// <summary>
/// In-memory queue backed by System.Threading.Channels.
/// Replace this class with an Azure Service Bus publisher/consumer later.
/// </summary>
public sealed class InMemoryPAQueue : IPAQueue
{
    private readonly Channel<PAQueueMessage> _channel = Channel.CreateUnbounded<PAQueueMessage>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

    public async Task PublishAsync(PAQueueMessage message, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public IAsyncEnumerable<PAQueueMessage> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
