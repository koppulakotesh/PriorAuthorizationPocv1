using Microsoft.Extensions.Logging;
using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Exceptions;
using PriorAuthorization.Application.Interfaces;
using PriorAuthorization.Domain.Entities;
using PriorAuthorization.Domain.Enums;

namespace PriorAuthorization.Application.Services;

/// <summary>
/// Orchestrates submit / get / update / cancel / delete.
/// Controllers call this class and nothing else.
/// </summary>
public class PriorAuthorizationService : IPriorAuthorizationService
{
    private readonly IPriorAuthorizationValidator _validator;
    private readonly IPriorAuthorizationRepository _repository;
    private readonly IPAQueue _queue;
    private readonly IPAAuditService _auditService;
    private readonly IFhirMapper _fhirMapper;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<PriorAuthorizationService> _logger;

    public PriorAuthorizationService(
        IPriorAuthorizationValidator validator,
        IPriorAuthorizationRepository repository,
        IPAQueue queue,
        IPAAuditService auditService,
        IFhirMapper fhirMapper,
        ICorrelationContext correlationContext,
        ILogger<PriorAuthorizationService> logger)
    {
        _validator = validator;
        _repository = repository;
        _queue = queue;
        _auditService = auditService;
        _fhirMapper = fhirMapper;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task<SubmitAcceptedResponse> SubmitAsync(
        FhirPriorAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = EnsureCorrelationId();

        _logger.LogInformation("PA request received. CorrelationId={CorrelationId}", correlationId);

        EnsureValid(request, correlationId);

        var canonical = _fhirMapper.ToCanonical(request, correlationId);
        var entity = ToEntity(canonical);
        entity.Status = nameof(PAStatus.Received);
        entity.CreatedOn = DateTime.UtcNow;
        entity.UpdatedOn = entity.CreatedOn;

        await _repository.CreateAsync(entity, cancellationToken);

        _logger.LogInformation(
            "PA {PAId} saved to database with status {Status}. CorrelationId={CorrelationId}",
            entity.Id,
            entity.Status,
            correlationId);

        await _auditService.RecordAsync(
            entity.Id,
            "Received",
            oldStatus: null,
            newStatus: entity.Status,
            message: "Prior authorization request received.",
            correlationId,
            cancellationToken);

        await PublishAsync(entity.Id, correlationId, PAQueueActions.Submit, cancellationToken);

        return new SubmitAcceptedResponse
        {
            Id = entity.Id,
            CorrelationId = correlationId,
            Status = entity.Status,
            Message = "Prior authorization request received"
        };
    }

    public async Task<FhirPriorAuthorizationResponse> GetAsync(long id, CancellationToken cancellationToken)
    {
        var correlationId = EnsureCorrelationId();

        _logger.LogInformation("PA inquiry started. PA {PAId}. CorrelationId={CorrelationId}", id, correlationId);

        var entity = await GetRequiredAsync(id, cancellationToken);
        var canonical = _fhirMapper.ToCanonical(entity);
        return _fhirMapper.ToFhirResponse(canonical);
    }

    public async Task<SubmitAcceptedResponse> UpdateAsync(
        long id,
        FhirPriorAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = EnsureCorrelationId();

        _logger.LogInformation("PA update received. PA {PAId}. CorrelationId={CorrelationId}", id, correlationId);

        EnsureValid(request, correlationId);

        var entity = await GetRequiredAsync(id, cancellationToken);
        if (entity.Status == nameof(PAStatus.Cancelled))
        {
            throw new ValidationException(["Cancelled prior authorizations cannot be updated."]);
        }

        var canonical = _fhirMapper.ToCanonical(request, correlationId);
        entity.PatientId = canonical.PatientId;
        entity.PatientName = canonical.PatientName;
        entity.ProviderId = canonical.ProviderId;
        entity.ProviderName = canonical.ProviderName;
        entity.InsuranceId = canonical.InsuranceId;
        entity.InsuranceName = canonical.InsuranceName;
        entity.ProcedureCode = canonical.ProcedureCode;
        entity.ProcedureDescription = canonical.ProcedureDescription;
        entity.CorrelationId = correlationId;
        entity.Status = nameof(PAStatus.Received);
        entity.LastError = null;
        entity.UpdatedOn = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, cancellationToken);

        await _auditService.RecordAsync(
            entity.Id,
            "Updated",
            oldStatus: null,
            newStatus: entity.Status,
            message: "Prior authorization request updated and queued.",
            correlationId,
            cancellationToken);

        await PublishAsync(entity.Id, correlationId, PAQueueActions.Update, cancellationToken);

        return new SubmitAcceptedResponse
        {
            Id = entity.Id,
            CorrelationId = correlationId,
            Status = entity.Status,
            Message = "Prior authorization update received"
        };
    }

    public async Task<SubmitAcceptedResponse> CancelAsync(long id, CancellationToken cancellationToken)
    {
        var correlationId = EnsureCorrelationId();

        _logger.LogInformation("PA cancel received. PA {PAId}. CorrelationId={CorrelationId}", id, correlationId);

        var entity = await GetRequiredAsync(id, cancellationToken);
        if (entity.Status == nameof(PAStatus.Cancelled))
        {
            throw new ValidationException(["Prior authorization is already cancelled."]);
        }

        entity.CorrelationId = correlationId;
        entity.UpdatedOn = DateTime.UtcNow;
        await _repository.UpdateAsync(entity, cancellationToken);

        await _auditService.RecordAsync(
            entity.Id,
            "CancelRequested",
            entity.Status,
            entity.Status,
            "Cancel requested and queued.",
            correlationId,
            cancellationToken);

        await PublishAsync(entity.Id, correlationId, PAQueueActions.Cancel, cancellationToken);

        return new SubmitAcceptedResponse
        {
            Id = entity.Id,
            CorrelationId = correlationId,
            Status = entity.Status,
            Message = "Prior authorization cancel received"
        };
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var correlationId = EnsureCorrelationId();
        var entity = await GetRequiredAsync(id, cancellationToken);

        await _repository.DeleteAsync(entity, cancellationToken);

        _logger.LogInformation("PA {PAId} deleted. CorrelationId={CorrelationId}", id, correlationId);
    }

    private void EnsureValid(FhirPriorAuthorizationRequest request, string correlationId)
    {
        var validation = _validator.Validate(request);
        if (validation.IsValid)
        {
            _logger.LogInformation("PA request validation succeeded. CorrelationId={CorrelationId}", correlationId);
            return;
        }

        _logger.LogWarning(
            "PA request validation failed. CorrelationId={CorrelationId} Errors={ErrorCount}",
            correlationId,
            validation.Errors.Count);

        throw new ValidationException(validation.Errors);
    }

    private async Task PublishAsync(long id, string correlationId, string action, CancellationToken cancellationToken)
    {
        await _queue.PublishAsync(
            new PAQueueMessage
            {
                PriorAuthorizationId = id,
                CorrelationId = correlationId,
                Action = action
            },
            cancellationToken);

        _logger.LogInformation(
            "PA {PAId} queued. Action={Action}. CorrelationId={CorrelationId}",
            id,
            action,
            correlationId);
    }

    private async Task<PriorAuthorizationRecord> GetRequiredAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException($"Prior authorization {id} was not found.");
        }

        return entity;
    }

    private string EnsureCorrelationId()
    {
        if (string.IsNullOrWhiteSpace(_correlationContext.CorrelationId))
        {
            _correlationContext.CorrelationId = Guid.NewGuid().ToString();
        }

        return _correlationContext.CorrelationId;
    }

    private static PriorAuthorizationRecord ToEntity(Domain.Models.CanonicalPriorAuthorization canonical)
    {
        return new PriorAuthorizationRecord
        {
            CorrelationId = canonical.CorrelationId,
            PatientId = canonical.PatientId,
            PatientName = canonical.PatientName,
            ProviderId = canonical.ProviderId,
            ProviderName = canonical.ProviderName,
            InsuranceId = canonical.InsuranceId,
            InsuranceName = canonical.InsuranceName,
            ProcedureCode = canonical.ProcedureCode,
            ProcedureDescription = canonical.ProcedureDescription
        };
    }
}
