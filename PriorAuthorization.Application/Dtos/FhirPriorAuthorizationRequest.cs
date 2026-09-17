namespace PriorAuthorization.Application.Dtos;

/// <summary>
/// Simplified FHIR-like request used by this POC. This is NOT the full FHIR spec.
/// </summary>
public class FhirPriorAuthorizationRequest
{
    public string? ResourceType { get; set; }
    public PatientDto? Patient { get; set; }
    public ProviderDto? Provider { get; set; }
    public InsuranceDto? Insurance { get; set; }
    public ProcedureDto? Procedure { get; set; }
}

public class PatientDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class ProviderDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class InsuranceDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class ProcedureDto
{
    public string? Code { get; set; }
    public string? Description { get; set; }
}
