using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WWSHA.Domain;
using WWSHA.Persistence.Commands;

namespace WWSHA.Api.Errors;

/// <summary>
/// Maps domain and persistence exceptions to RFC 7807 problem responses. Each response carries a
/// stable <c>code</c> extension (for example <c>hero.unavailable</c>) that clients can branch on.
/// Unexpected exceptions become a generic 500 with no internal details.
/// </summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Not found", "entity.not_found"),

            HeroUnavailableException
                or AssignmentFullException
                or DuplicateAssignmentException
                or IncidentClosedException
                or InvalidIncidentStateException => (StatusCodes.Status409Conflict, "Conflict",
                    ((DomainException)exception).Code),

            DomainException domain => (StatusCodes.Status400BadRequest, "Invalid request", domain.Code),

            // Malformed bodies (e.g. an unknown enum name) surface as BadHttpRequestException.
            ArgumentException or BadHttpRequestException
                => (StatusCodes.Status400BadRequest, "Invalid request", "request.invalid"),

            _ => (StatusCodes.Status500InternalServerError, "Internal server error", "server.error")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
