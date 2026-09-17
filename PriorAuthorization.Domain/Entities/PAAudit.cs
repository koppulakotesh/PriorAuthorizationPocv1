namespace PriorAuthorization.Domain.Entities;

/// <summary>
/// One audit row for an important event or status change.
/// </summary>
public class PAAudit
{
    public long Id { get; set; }
    public long PriorAuthorizationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}
