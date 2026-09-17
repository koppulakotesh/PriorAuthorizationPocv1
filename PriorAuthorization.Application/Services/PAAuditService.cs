using Microsoft.Extensions.Logging;
using PriorAuthorization.Application.Interfaces;
using PriorAuthorization.Domain.Entities;

namespace PriorAuthorization.Application.Services;

public class PAAuditService : IPAAuditService
{
    private readonly IPriorAuthorizationRepository _repository;
    private readonly ILogger<PAAuditService> _logger;

    public PAAuditService(
        IPriorAuthorizationRepository repository,
        ILogger<PAAuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RecordAsync(
        long priorAuthorizationId,
        string eventType,
        string? oldStatus,
        string? newStatus,
        string message,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var audit = new PAAudit
        {
            PriorAuthorizationId = priorAuthorizationId,
            EventType = eventType,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Message = message,
            CorrelationId = correlationId,
            CreatedOn = DateTime.UtcNow
        };

        await _repository.SaveAuditAsync(audit, cancellationToken);

        _logger.LogInformation(
            "Audit saved. PA {PAId} Event={EventType} {OldStatus} -> {NewStatus}. CorrelationId={CorrelationId}",
            priorAuthorizationId,
            eventType,
            oldStatus ?? "(none)",
            newStatus ?? "(none)",
            correlationId);
    }
}
