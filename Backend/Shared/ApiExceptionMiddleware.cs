using Microsoft.AspNetCore.Http;

namespace Backend.Shared;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger)
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
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (status, code) = exception switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found"),
                ArgumentException => (StatusCodes.Status400BadRequest, "invalid_request"),
                InvalidOperationException => (StatusCodes.Status409Conflict, "invalid_state"),
                _ => (StatusCodes.Status500InternalServerError, "internal_error"),
            };

            if (status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
            }

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Status = status,
                Code = code,
                Message = status >= StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception.Message,
            });
        }
    }
}
