using System.Threading;

namespace PriorAuthorization.Application.Services;

/// <summary>
/// Holds the current CorrelationId for this async flow (API request or worker message).
/// AsyncLocal keeps the value isolated per request / worker iteration.
/// </summary>
public sealed class CorrelationContext : Interfaces.ICorrelationContext
{
    private static readonly AsyncLocal<string?> Current = new();

    public string CorrelationId
    {
        get => Current.Value ?? string.Empty;
        set => Current.Value = value;
    }
}
