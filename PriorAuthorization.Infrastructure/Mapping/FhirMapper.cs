using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Interfaces;
using PriorAuthorization.Domain.Entities;
using PriorAuthorization.Domain.Models;

namespace PriorAuthorization.Infrastructure.Mapping;

/// <summary>
/// Explicit mapping. No AutoMapper, so the conversion is easy to read.
/// FHIR request  -> canonical model
/// DB entity     -> canonical model
/// canonical     -> FHIR response
/// </summary>
public class FhirMapper : IFhirMapper
{
    public CanonicalPriorAuthorization ToCanonical(FhirPriorAuthorizationRequest request, string correlationId)
    {
        return new CanonicalPriorAuthorization
        {
            ResourceType = request.ResourceType ?? "PriorAuthorization",
            CorrelationId = correlationId,
            PatientId = request.Patient?.Id ?? string.Empty,
            PatientName = request.Patient?.Name,
            ProviderId = request.Provider?.Id ?? string.Empty,
            ProviderName = request.Provider?.Name,
            InsuranceId = request.Insurance?.Id ?? string.Empty,
            InsuranceName = request.Insurance?.Name,
            ProcedureCode = request.Procedure?.Code ?? string.Empty,
            ProcedureDescription = request.Procedure?.Description
        };
    }

    public CanonicalPriorAuthorization ToCanonical(PriorAuthorizationRecord entity)
    {
        return new CanonicalPriorAuthorization
        {
            Id = entity.Id,
            ResourceType = "PriorAuthorization",
            CorrelationId = entity.CorrelationId,
            Status = entity.Status,
            PatientId = entity.PatientId,
            PatientName = entity.PatientName,
            ProviderId = entity.ProviderId,
            ProviderName = entity.ProviderName,
            InsuranceId = entity.InsuranceId,
            InsuranceName = entity.InsuranceName,
            ProcedureCode = entity.ProcedureCode,
            ProcedureDescription = entity.ProcedureDescription,
            ExternalReferenceId = entity.ExternalReferenceId,
            LastError = entity.LastError
        };
    }

    public FhirPriorAuthorizationResponse ToFhirResponse(CanonicalPriorAuthorization model)
    {
        return new FhirPriorAuthorizationResponse
        {
            ResourceType = "PriorAuthorization",
            Id = model.Id ?? 0,
            Status = model.Status,
            CorrelationId = model.CorrelationId,
            ExternalReferenceId = model.ExternalReferenceId,
            Patient = new PatientDto
            {
                Id = model.PatientId,
                Name = model.PatientName
            },
            Provider = new ProviderDto
            {
                Id = model.ProviderId,
                Name = model.ProviderName
            },
            Insurance = new InsuranceDto
            {
                Id = model.InsuranceId,
                Name = model.InsuranceName
            },
            Procedure = new ProcedureDto
            {
                Code = model.ProcedureCode,
                Description = model.ProcedureDescription
            }
        };
    }
}
