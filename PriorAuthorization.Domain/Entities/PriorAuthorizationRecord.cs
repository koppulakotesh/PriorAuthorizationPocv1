namespace PriorAuthorization.Domain.Entities;

/// <summary>
/// Prior Authorization database entity (named Record so it does not clash with the PriorAuthorization namespace).
/// Extra name/description fields are stored so GET can return a FHIR-like payload.
/// </summary>
public class PriorAuthorizationRecord
{
    public long Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public string InsuranceId { get; set; } = string.Empty;
    public string? InsuranceName { get; set; }
    public string ProcedureCode { get; set; } = string.Empty;
    public string? ProcedureDescription { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExternalReferenceId { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public string? LastError { get; set; }
}
