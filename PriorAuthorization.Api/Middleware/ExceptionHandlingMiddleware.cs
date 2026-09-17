using System.Net;
using System.Text.Json;
using PriorAuthorization.Application.Dtos;
using PriorAuthorization.Application.Exceptions;
using PriorAuthorization.Application.Interfaces;

namespace PriorAuthorization.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = correlationContext.CorrelationId;
            _logger.LogError(ex, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);
            await WriteErrorAsync(context, ex, correlationId);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, Exception exception, string correlationId)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validation =>
                (HttpStatusCode.BadRequest, validation.Message, validation.Errors),
            NotFoundException notFound =>
                (HttpStatusCode.NotFound, notFound.Message, (IReadOnlyList<string>?)null),
            _ => (HttpStatusCode.InternalServerError, "Something went wrong", null)
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = new ApiErrorResponse
        {
            Success = false,
            Message = message,
            CorrelationId = correlationId,
            Errors = errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
