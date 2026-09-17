using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Interfaces;

namespace PriorAuthorization.Application.Validators;

public class PriorAuthorizationValidator : IPriorAuthorizationValidator
{
    public ValidationResult Validate(FhirPriorAuthorizationRequest request)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(request.ResourceType))
        {
            result.Errors.Add("resourceType is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Patient?.Id))
        {
            result.Errors.Add("patient.id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Provider?.Id))
        {
            result.Errors.Add("provider.id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Insurance?.Id))
        {
            result.Errors.Add("insurance.id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Procedure?.Code))
        {
            result.Errors.Add("procedure.code is required.");
        }

        return result;
    }
}
