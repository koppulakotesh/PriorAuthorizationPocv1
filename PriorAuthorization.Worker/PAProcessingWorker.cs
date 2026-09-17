using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriorAuthorization.Application.Interfaces;

namespace PriorAuthorization.Worker;

/// <summary>
/// Reads queue messages and hands each one to IPriorAuthorizationProcessor.
/// The worker itself does not contain business logic.
/// </summary>
public class PAProcessingWorker : BackgroundService
{
    private readonly IPAQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PAProcessingWorker> _logger;

    public PAProcessingWorker(
        IPAQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PAProcessingWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PA processing worker started.");

        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IPriorAuthorizationProcessor>();
                await processor.ProcessAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled worker error for PA {PAId}. CorrelationId={CorrelationId}",
                    message.PriorAuthorizationId,
                    message.CorrelationId);
            }
        }
    }
}
