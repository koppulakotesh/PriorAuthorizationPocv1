using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Domain.Entities;
using PriorAuthorization.Domain.Models;

namespace PriorAuthorization.Application.Interfaces;

public interface IFhirMapper
{
    CanonicalPriorAuthorization ToCanonical(FhirPriorAuthorizationRequest request, string correlationId);
    CanonicalPriorAuthorization ToCanonical(PriorAuthorizationRecord entity);
    FhirPriorAuthorizationResponse ToFhirResponse(CanonicalPriorAuthorization model);
}
