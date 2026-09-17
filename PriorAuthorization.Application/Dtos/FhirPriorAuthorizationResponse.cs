namespace PriorAuthorization.Application.Dtos;

/// <summary>
/// Simplified FHIR-like response returned by GET inquiry.
/// </summary>
public class FhirPriorAuthorizationResponse
{
    public string ResourceType { get; set; } = "PriorAuthorization";
    public long Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? ExternalReferenceId { get; set; }
    public PatientDto? Patient { get; set; }
    public ProviderDto? Provider { get; set; }
    public InsuranceDto? Insurance { get; set; }
    public ProcedureDto? Procedure { get; set; }
}
