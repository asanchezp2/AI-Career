using Microsoft.AspNetCore.Mvc;

namespace FraudDetection.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions,
/// logs them with structured data, and returns an RFC 7807 ProblemDetails response.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Creates a new ExceptionHandlingMiddleware with the given next delegate and logger.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware, wrapping the downstream pipeline in a try/catch.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Detail = "The server encountered an unexpected error. Please try again later.",
                Extensions = { ["requestId"] = context.TraceIdentifier }
            };

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            // Pass the content type explicitly: the options-less overload would
            // otherwise overwrite any pre-set ContentType with "application/json".
            await context.Response.WriteAsJsonAsync(
                problemDetails,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
        }
    }
}
