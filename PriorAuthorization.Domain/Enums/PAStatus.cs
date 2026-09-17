namespace PriorAuthorization.Domain.Enums;

/// <summary>
/// Simple prior-authorization lifecycle. Kept as a small, readable set for the POC.
/// </summary>
public enum PAStatus
{
    Received,
    Processing,
    Submitted,
    Approved,
    Rejected,
    Cancelled,
    Failed
}
