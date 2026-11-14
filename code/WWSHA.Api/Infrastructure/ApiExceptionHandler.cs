using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WWSHA.Domain;

namespace WWSHA.Api.Infrastructure;

/// <summary>
/// Converts domain and request exceptions into RFC 7807 <see cref="ProblemDetails"/> responses.
/// </summary>
public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            DomainValidationException => (StatusCodes.Status400BadRequest, "Bad Request"),
            HeroUnavailableException => (StatusCodes.Status409Conflict, "Conflict"),
            IncidentClosedException => (StatusCodes.Status409Conflict, "Conflict"),
            AssignmentFullException => (StatusCodes.Status409Conflict, "Conflict"),
            DuplicateAssignmentException => (StatusCodes.Status409Conflict, "Conflict"),
            InvalidIncidentStateException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status400BadRequest, "Bad Request"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = exception switch
        {
            DomainException domainException => domainException.Code,
            ArgumentException => "request.invalid",
            _ => "server.error"
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
