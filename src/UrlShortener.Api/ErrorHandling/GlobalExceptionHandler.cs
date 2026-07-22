using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Core.Exceptions;

namespace UrlShortener.Api.ErrorHandling;

/// <summary>
/// Catches any exception that reaches the top of the pipeline and turns it
/// into an RFC 7807 ProblemDetails response instead of a bare 500 page.
/// Expected failures (validation, not-found) are handled directly in
/// controllers and never reach this handler.
/// </summary>
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            ShortCodeGenerationException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError,
        };

        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode == StatusCodes.Status503ServiceUnavailable
                    ? "Service temporarily unavailable"
                    : "An unexpected error occurred",
                Type = $"https://tools.ietf.org/html/rfc9110#section-15.6.{(statusCode == 503 ? 4 : 1)}",
            },
        });
    }
}
