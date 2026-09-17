using PriorAuthorization.Application.Dtos;

namespace PriorAuthorization.Application.Interfaces;

public interface IPriorAuthorizationService
{
    Task<SubmitAcceptedResponse> SubmitAsync(FhirPriorAuthorizationRequest request, CancellationToken cancellationToken);
    Task<FhirPriorAuthorizationResponse> GetAsync(long id, CancellationToken cancellationToken);
    Task<SubmitAcceptedResponse> UpdateAsync(long id, FhirPriorAuthorizationRequest request, CancellationToken cancellationToken);
    Task<SubmitAcceptedResponse> CancelAsync(long id, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
