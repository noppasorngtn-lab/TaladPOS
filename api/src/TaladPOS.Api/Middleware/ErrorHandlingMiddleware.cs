using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaladPOS.Domain.Exceptions;

namespace TaladPOS.Api.Middleware;

/// <summary>
/// Produces the `{ "error": { "code", "message" } }` shape from contracts/README.md,
/// mapping domain exceptions to 409 (concurrency/state conflicts), 422 (validation failures),
/// and 404 (missing entities looked up by id).
/// </summary>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message) = exception switch
        {
            DomainConflictException conflict => (HttpStatusCode.Conflict, conflict.Code, conflict.Message),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "concurrency_conflict", "The record was modified by another request. Please retry."),
            KeyNotFoundException notFound => (HttpStatusCode.NotFound, "not_found", notFound.Message),
            ArgumentException argumentException => (HttpStatusCode.UnprocessableEntity, "validation_error", argumentException.Message),
            _ => (HttpStatusCode.InternalServerError, "internal_error", "An unexpected error occurred."),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new { error = new { code, message } };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
