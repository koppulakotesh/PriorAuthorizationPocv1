namespace PriorAuthorization.Application.Dtos;

public class ApiErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public IReadOnlyList<string>? Errors { get; set; }
}
