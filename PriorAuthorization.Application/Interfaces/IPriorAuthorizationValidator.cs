using PriorAuthorization.Application.Dtos;

namespace PriorAuthorization.Application.Interfaces;

public interface IPriorAuthorizationValidator
{
    ValidationResult Validate(FhirPriorAuthorizationRequest request);
}
