namespace PriorAuthorization.Application.Interfaces;

public interface IPAAuditService
{
    Task RecordAsync(
        long priorAuthorizationId,
        string eventType,
        string? oldStatus,
        string? newStatus,
        string message,
        string correlationId,
        CancellationToken cancellationToken);
}
