using Microsoft.Extensions.Logging;
using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Interfaces;
using PriorAuthorization.Domain.Models;

namespace PriorAuthorization.Infrastructure.External;

/// <summary>
/// Fake QAR API. Production code would POST JSON with HttpClient.
/// </summary>
public class MockQarClient : IQarClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockQarClient> _logger;

    public MockQarClient(HttpClient httpClient, ILogger<MockQarClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<QarSubmitResponse> SubmitPriorAuthorizationAsync(
        CanonicalPriorAuthorization request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        return SendMockAsync("submit", request, correlationId, cancellationToken);
    }

    public Task<QarSubmitResponse> CancelPriorAuthorizationAsync(
        CanonicalPriorAuthorization request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        return SendMockAsync("cancel", request, correlationId, cancellationToken);
    }

    private async Task<QarSubmitResponse> SendMockAsync(
        string action,
        CanonicalPriorAuthorization request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Calling mock QAR {Action} for PA {PAId}. Target={Target}. CorrelationId={CorrelationId}",
            action,
            request.Id,
            _httpClient.BaseAddress,
            correlationId);

        await Task.Delay(1000, cancellationToken);

        var response = new QarSubmitResponse
        {
            Success = true,
            ExternalReferenceId = request.ExternalReferenceId ?? "QAR-10001",
            Status = action == "cancel" ? "Cancelled" : "Submitted"
        };

        _logger.LogInformation(
            "Mock QAR {Action} succeeded. ExternalReferenceId={ExternalReferenceId}. CorrelationId={CorrelationId}",
            action,
            response.ExternalReferenceId,
            correlationId);

        return response;
    }
}
