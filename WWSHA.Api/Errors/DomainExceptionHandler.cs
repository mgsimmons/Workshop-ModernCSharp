using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WWSHA.Domain;
using WWSHA.Persistence.Commands;

namespace WWSHA.Api.Errors;

/// <summary>
/// Maps domain and persistence exceptions to HTTP status codes:
/// not found -> 404, state or capacity conflicts -> 409, invalid input -> 400.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Not found"),

            HeroUnavailableException
                or AssignmentFullException
                or InvalidIncidentStateException => (StatusCodes.Status409Conflict, "Conflict"),

            InvalidReportException
                or ArgumentException
                or DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),

            _ => (0, string.Empty)
        };

        if (status == 0)
            return false; // let ASP.NET Core log and return a 500

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
