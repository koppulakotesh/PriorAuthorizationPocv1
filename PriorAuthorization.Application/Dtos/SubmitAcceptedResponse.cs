namespace PriorAuthorization.Application.Dtos;

/// <summary>
/// 202 Accepted payload returned by submit, update, and cancel.
/// </summary>
public class SubmitAcceptedResponse
{
    public long Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
