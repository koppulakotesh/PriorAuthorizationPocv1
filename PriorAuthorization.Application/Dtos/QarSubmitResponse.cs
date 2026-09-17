namespace PriorAuthorization.Application.Dtos;

public class QarSubmitResponse
{
    public bool Success { get; set; }
    public string ExternalReferenceId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}
