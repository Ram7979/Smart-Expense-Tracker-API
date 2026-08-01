using System.Net;
using System.Text.Json;

namespace ExpenseTracker.Api.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions into a consistent
/// JSON error shape: { "error": "message" }. Logs the full exception for debugging.
/// This avoids leaking stack traces to clients in production.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(new
            {
                error = "An unexpected error occurred. Please try again later."
            });

            await context.Response.WriteAsync(body);
        }
    }
}
