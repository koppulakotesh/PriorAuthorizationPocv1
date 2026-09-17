using Microsoft.AspNetCore.Mvc;
using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Interfaces;

namespace PriorAuthorization.Api.Controllers;

[ApiController]
[Route("api/prior-authorizations")]
[Produces("application/json")]
public class PriorAuthorizationController : ControllerBase
{
    private readonly IPriorAuthorizationService _service;

    public PriorAuthorizationController(IPriorAuthorizationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Submit a FHIR-like prior authorization request.
    /// Saves the record, queues background processing, and returns 202 Accepted.
    /// Does not call QAR from the API.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/prior-authorizations
    ///     {
    ///         "resourceType": "PriorAuthorization",
    ///         "patient": { "id": "PAT001", "name": "John Doe" },
    ///         "provider": { "id": "PROV001", "name": "ABC Hospital" },
    ///         "insurance": { "id": "INS001", "name": "ABC Insurance" },
    ///         "procedure": { "code": "MRI001", "description": "MRI Scan" }
    ///     }
    ///
    /// Sample response:
    ///
    ///     {
    ///         "id": 1,
    ///         "correlationId": "some-guid",
    ///         "status": "Received",
    ///         "message": "Prior authorization request received"
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(SubmitAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(
        [FromBody] FhirPriorAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SubmitAsync(request, cancellationToken);
        return Accepted(result);
    }

    /// <summary>
    /// Inquiry: get the current prior authorization as a FHIR-like response.
    /// This is synchronous and does not use the queue.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(FhirPriorAuthorizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update an existing prior authorization and re-queue it for QAR processing.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(SubmitAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] FhirPriorAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return Accepted(result);
    }

    /// <summary>
    /// Delete a prior authorization record. This is a simple POC delete.
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cancel a prior authorization. The API validates, queues the cancel, and returns 202.
    /// The worker calls mock QAR and then updates the database.
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(typeof(SubmitAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        var result = await _service.CancelAsync(id, cancellationToken);
        return Accepted(result);
    }
}
