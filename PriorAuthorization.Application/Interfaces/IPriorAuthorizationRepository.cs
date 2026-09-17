using PriorAuthorization.Domain.Entities;

namespace PriorAuthorization.Application.Interfaces;

public interface IPriorAuthorizationRepository
{
    Task<PriorAuthorizationRecord> CreateAsync(PriorAuthorizationRecord entity, CancellationToken cancellationToken);
    Task<PriorAuthorizationRecord?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task UpdateAsync(PriorAuthorizationRecord entity, CancellationToken cancellationToken);
    Task DeleteAsync(PriorAuthorizationRecord entity, CancellationToken cancellationToken);
    Task SaveAuditAsync(PAAudit audit, CancellationToken cancellationToken);
}
