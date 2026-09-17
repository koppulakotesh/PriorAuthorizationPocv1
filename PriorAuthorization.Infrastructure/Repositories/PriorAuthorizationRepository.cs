using Microsoft.EntityFrameworkCore;
using PriorAuthorization.Application.Interfaces;
using PriorAuthorization.Domain.Entities;
using PriorAuthorization.Infrastructure.Data;

namespace PriorAuthorization.Infrastructure.Repositories;

public class PriorAuthorizationRepository : IPriorAuthorizationRepository
{
    private readonly PADbContext _dbContext;

    public PriorAuthorizationRepository(PADbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PriorAuthorizationRecord> CreateAsync(PriorAuthorizationRecord entity, CancellationToken cancellationToken)
    {
        _dbContext.PriorAuthorizations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public Task<PriorAuthorizationRecord?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.PriorAuthorizations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(PriorAuthorizationRecord entity, CancellationToken cancellationToken)
    {
        _dbContext.PriorAuthorizations.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PriorAuthorizationRecord entity, CancellationToken cancellationToken)
    {
        _dbContext.PriorAuthorizations.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAuditAsync(PAAudit audit, CancellationToken cancellationToken)
    {
        _dbContext.PAAudits.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
