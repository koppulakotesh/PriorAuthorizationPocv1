namespace PriorAuthorization.Application.Dtos;

public class ServiceNowTicketResponse
{
    public bool Success { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string? Error { get; set; }
}
