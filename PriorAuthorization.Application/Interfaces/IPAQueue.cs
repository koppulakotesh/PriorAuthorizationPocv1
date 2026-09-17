using PriorAuthorization.Application.Dtos;

namespace PriorAuthorization.Application.Interfaces;

/// <summary>
/// Abstraction over the work queue. Today this is an in-memory Channel.
/// Tomorrow it can be Azure Service Bus without changing callers.
/// </summary>
public interface IPAQueue
{
    Task PublishAsync(PAQueueMessage message, CancellationToken cancellationToken);
    IAsyncEnumerable<PAQueueMessage> ReadAllAsync(CancellationToken cancellationToken);
}
