using Microsoft.Extensions.Logging;
using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Interfaces;
using PriorAuthorization.Domain.Models;

namespace PriorAuthorization.Infrastructure.External;

/// <summary>
/// Fake ServiceNow API. Production code would POST JSON with HttpClient.
/// </summary>
public class MockServiceNowClient : IServiceNowClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockServiceNowClient> _logger;

    public MockServiceNowClient(HttpClient httpClient, ILogger<MockServiceNowClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ServiceNowTicketResponse> CreateOrUpdateTicketAsync(
        CanonicalPriorAuthorization request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Calling mock ServiceNow for PA {PAId}. Target={Target}. CorrelationId={CorrelationId}",
            request.Id,
            _httpClient.BaseAddress,
            correlationId);

        await Task.Delay(300, cancellationToken);

        var response = new ServiceNowTicketResponse
        {
            Success = true,
            TicketNumber = "INC0010001"
        };

        _logger.LogInformation(
            "Mock ServiceNow succeeded. TicketNumber={TicketNumber}. CorrelationId={CorrelationId}",
            response.TicketNumber,
            correlationId);

        return response;
    }
}
