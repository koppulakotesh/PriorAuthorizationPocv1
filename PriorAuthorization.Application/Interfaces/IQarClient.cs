using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Domain.Models;

namespace PriorAuthorization.Application.Interfaces;

public interface IQarClient
{
    Task<QarSubmitResponse> SubmitPriorAuthorizationAsync(
        CanonicalPriorAuthorization request,
        string correlationId,
        CancellationToken cancellationToken);

    Task<QarSubmitResponse> CancelPriorAuthorizationAsync(
        CanonicalPriorAuthorization request,
        string correlationId,
        CancellationToken cancellationToken);
}
