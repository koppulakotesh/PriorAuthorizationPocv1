namespace PriorAuthorization.Application.Interfaces;

public interface ICorrelationContext
{
    string CorrelationId { get; set; }
}
