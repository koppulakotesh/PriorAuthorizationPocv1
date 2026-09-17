namespace PriorAuthorization.Domain.Models;

/// <summary>
/// Internal canonical model used after FHIR mapping and before FHIR response mapping.
/// This is the "business" shape the rest of the app understands.
/// </summary>
public class CanonicalPriorAuthorization
{
    public long? Id { get; set; }
    public string ResourceType { get; set; } = "PriorAuthorization";
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public string InsuranceId { get; set; } = string.Empty;
    public string? InsuranceName { get; set; }
    public string ProcedureCode { get; set; } = string.Empty;
    public string? ProcedureDescription { get; set; }
    public string? ExternalReferenceId { get; set; }
    public string? LastError { get; set; }
}
