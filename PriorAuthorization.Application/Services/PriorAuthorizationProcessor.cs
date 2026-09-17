using Microsoft.Extensions.Logging;
using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Interfaces;
using PriorAuthorization.Domain.Entities;
using PriorAuthorization.Domain.Enums;
using PriorAuthorization.Domain.Models;

namespace PriorAuthorization.Application.Services;

/// <summary>
/// Background processing use case. The worker calls this; it does not live in the worker class.
/// </summary>
public class PriorAuthorizationProcessor : IPriorAuthorizationProcessor
{
    private const int MaxQarAttempts = 3;

    private readonly IPriorAuthorizationRepository _repository;
    private readonly IPAAuditService _auditService;
    private readonly IFhirMapper _fhirMapper;
    private readonly IQarClient _qarClient;
    private readonly IServiceNowClient _serviceNowClient;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<PriorAuthorizationProcessor> _logger;

    public PriorAuthorizationProcessor(
        IPriorAuthorizationRepository repository,
        IPAAuditService auditService,
        IFhirMapper fhirMapper,
        IQarClient qarClient,
        IServiceNowClient serviceNowClient,
        ICorrelationContext correlationContext,
        ILogger<PriorAuthorizationProcessor> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _fhirMapper = fhirMapper;
        _qarClient = qarClient;
        _serviceNowClient = serviceNowClient;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task ProcessAsync(PAQueueMessage message, CancellationToken cancellationToken)
    {
        _correlationContext.CorrelationId = message.CorrelationId;

        _logger.LogInformation(
            "Worker received message. PA {PAId} Action={Action}. CorrelationId={CorrelationId}",
            message.PriorAuthorizationId,
            message.Action,
            message.CorrelationId);

        var entity = await _repository.GetByIdAsync(message.PriorAuthorizationId, cancellationToken);
        if (entity is null)
        {
            _logger.LogWarning(
                "Worker skipped missing PA {PAId}. CorrelationId={CorrelationId}",
                message.PriorAuthorizationId,
                message.CorrelationId);
            return;
        }

        await ChangeStatusAsync(
            entity,
            nameof(PAStatus.Processing),
            "Processing started.",
            cancellationToken);

        var canonical = _fhirMapper.ToCanonical(entity);

        try
        {
            if (message.Action == PAQueueActions.Cancel)
            {
                await ProcessCancelAsync(entity, canonical, cancellationToken);
                return;
            }

            await ProcessSubmitOrUpdateAsync(entity, canonical, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Worker failed for PA {PAId}. CorrelationId={CorrelationId}",
                entity.Id,
                entity.CorrelationId);

            await FailAsync(entity, ex.Message, cancellationToken);
        }
    }

    private async Task ProcessSubmitOrUpdateAsync(
        PriorAuthorizationRecord entity,
        CanonicalPriorAuthorization canonical,
        CancellationToken cancellationToken)
    {
        var qarResult = await SubmitToQarWithRetryAsync(canonical, entity.CorrelationId, cancellationToken);
        if (!qarResult.Success)
        {
            await FailAsync(entity, qarResult.Error ?? "QAR submission failed.", cancellationToken);
            return;
        }

        entity.ExternalReferenceId = qarResult.ExternalReferenceId;
        await ChangeStatusAsync(
            entity,
            nameof(PAStatus.Submitted),
            $"Submitted to QAR. ExternalReferenceId={qarResult.ExternalReferenceId}",
            cancellationToken);

        canonical.Status = entity.Status;
        canonical.ExternalReferenceId = entity.ExternalReferenceId;
        await CallServiceNowAsync(canonical, entity.CorrelationId, cancellationToken);
    }

    private async Task ProcessCancelAsync(
        PriorAuthorizationRecord entity,
        CanonicalPriorAuthorization canonical,
        CancellationToken cancellationToken)
    {
        var qarResult = await _qarClient.CancelPriorAuthorizationAsync(
            canonical,
            entity.CorrelationId,
            cancellationToken);

        if (!qarResult.Success)
        {
            await FailAsync(entity, qarResult.Error ?? "QAR cancel failed.", cancellationToken);
            return;
        }

        await ChangeStatusAsync(
            entity,
            nameof(PAStatus.Cancelled),
            "Cancelled in QAR.",
            cancellationToken);

        canonical.Status = entity.Status;
        await CallServiceNowAsync(canonical, entity.CorrelationId, cancellationToken);
    }

    private async Task<QarSubmitResponse> SubmitToQarWithRetryAsync(
        CanonicalPriorAuthorization canonical,
        string correlationId,
        CancellationToken cancellationToken)
    {
        QarSubmitResponse? lastResult = null;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxQarAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "QAR submit attempt {Attempt}/{Max} for PA {PAId}. CorrelationId={CorrelationId}",
                    attempt,
                    MaxQarAttempts,
                    canonical.Id,
                    correlationId);

                lastResult = await _qarClient.SubmitPriorAuthorizationAsync(canonical, correlationId, cancellationToken);
                if (lastResult.Success)
                {
                    return lastResult;
                }

                _logger.LogWarning(
                    "QAR submit attempt {Attempt} failed for PA {PAId}. CorrelationId={CorrelationId}",
                    attempt,
                    canonical.Id,
                    correlationId);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "QAR submit attempt {Attempt} threw for PA {PAId}. CorrelationId={CorrelationId}",
                    attempt,
                    canonical.Id,
                    correlationId);
            }

            if (attempt < MaxQarAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
        }

        return lastResult ?? new QarSubmitResponse
        {
            Success = false,
            Error = lastException?.Message ?? "QAR submission failed after 3 attempts."
        };
    }

    private async Task CallServiceNowAsync(
        CanonicalPriorAuthorization canonical,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Calling ServiceNow for PA {PAId}. CorrelationId={CorrelationId}",
            canonical.Id,
            correlationId);

        var ticket = await _serviceNowClient.CreateOrUpdateTicketAsync(canonical, correlationId, cancellationToken);

        _logger.LogInformation(
            "ServiceNow ticket {TicketNumber} for PA {PAId}. CorrelationId={CorrelationId}",
            ticket.TicketNumber,
            canonical.Id,
            correlationId);

        await _auditService.RecordAsync(
            canonical.Id ?? 0,
            "ServiceNowUpdated",
            canonical.Status,
            canonical.Status,
            $"ServiceNow ticket {ticket.TicketNumber}.",
            correlationId,
            cancellationToken);
    }

    private async Task ChangeStatusAsync(
        PriorAuthorizationRecord entity,
        string newStatus,
        string message,
        CancellationToken cancellationToken)
    {
        var oldStatus = entity.Status;
        entity.Status = newStatus;
        entity.UpdatedOn = DateTime.UtcNow;
        await _repository.UpdateAsync(entity, cancellationToken);

        _logger.LogInformation(
            "Status change PA {PAId}: {OldStatus} -> {NewStatus}. CorrelationId={CorrelationId}",
            entity.Id,
            oldStatus,
            newStatus,
            entity.CorrelationId);

        await _auditService.RecordAsync(
            entity.Id,
            "StatusChanged",
            oldStatus,
            newStatus,
            message,
            entity.CorrelationId,
            cancellationToken);
    }

    private async Task FailAsync(PriorAuthorizationRecord entity, string error, CancellationToken cancellationToken)
    {
        entity.LastError = error;
        await ChangeStatusAsync(
            entity,
            nameof(PAStatus.Failed),
            $"Processing failed: {error}",
            cancellationToken);
    }
}
