using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Domain.Models;

namespace PriorAuthorization.Application.Interfaces;

public interface IServiceNowClient
{
    Task<ServiceNowTicketResponse> CreateOrUpdateTicketAsync(
        CanonicalPriorAuthorization request,
        string correlationId,
        CancellationToken cancellationToken);
}
