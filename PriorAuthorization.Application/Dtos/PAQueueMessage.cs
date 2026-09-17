namespace PriorAuthorization.Application.Dtos;

/// <summary>
/// Lightweight queue payload. Later this can be sent to Azure Service Bus unchanged.
/// </summary>
public class PAQueueMessage
{
    public long PriorAuthorizationId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Action { get; set; } = PAQueueActions.Submit;
}

public static class PAQueueActions
{
    public const string Submit = "Submit";
    public const string Update = "Update";
    public const string Cancel = "Cancel";
}
