using System.Text.Json;
using FraudDetection.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraudDetection.UnitTests.Api;

/// <summary>
/// Unit tests for the global exception handling middleware.
/// The middleware is exercised directly with a DefaultHttpContext and a
/// throwing RequestDelegate — cleaner and more reliable than forcing an
/// unhandled exception through the full API pipeline.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NextThrows_Returns500ProblemDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate throwingNext = _ => throw new InvalidOperationException("boom");
        var middleware = new ExceptionHandlingMiddleware(
            throwingNext,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — status 500 with RFC 7807 content type
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains("application/problem+json", context.Response.ContentType);

        // Assert — body is a ProblemDetails document with title and requestId
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("\"title\"", body);
        Assert.Contains("\"requestId\"", body);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(StatusCodes.Status500InternalServerError, root.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred", root.GetProperty("title").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("requestId").GetString()));
    }

    [Fact]
    public async Task InvokeAsync_NextSucceeds_ResponseUnaffected()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate succeedingNext = httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };
        var middleware = new ExceptionHandlingMiddleware(
            succeedingNext,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — middleware passes through without modification
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ClientAbortsRequest_RethrowsWithoutWritingResponse()
    {
        // Arrange — the request is aborted by the client before a response is produced
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        context.RequestAborted = cts.Token;

        RequestDelegate abortingNext = _ => throw new OperationCanceledException(cts.Token);
        var middleware = new ExceptionHandlingMiddleware(
            abortingNext,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act — a client abort must propagate instead of becoming a 500
        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));

        // Assert — no response was attempted on the dead connection
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }
}
