using PriorAuthorization.Application.Dtos;

namespace PriorAuthorization.Application.Interfaces;

public interface IPriorAuthorizationProcessor
{
    Task ProcessAsync(PAQueueMessage message, CancellationToken cancellationToken);
}
