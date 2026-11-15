using WWSHA.Api.Contracts;
using WWSHA.Api.Services;
using WWSHA.Domain;

namespace WWSHA.Api.Endpoints;

public static class AssignmentEndpoints
{
    public static void MapAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assignments").WithTags("Assignments");

        // GET /api/assignments?incidentId=&heroId=&open={bool?} -> AssignmentDto[]
        // "open" means the assignment's incident is not yet Closed.
        group.MapGet("/", (Guid? incidentId, Guid? heroId, bool? open, DomainService domain) =>
        {
            var assignments = domain.Query(ctx => ctx.Assignments
                .Where(a => incidentId is null || a.Incident.Id == incidentId)
                .Where(a => heroId is null || a.Heroes.Any(h => h.Id == heroId))
                .Where(a => open is null || (a.Incident.Status != IncidentStatus.Closed) == open)
                .Select(a => a.ToDto())
                .ToArray());
            return Results.Ok(assignments);
        });

        // GET /api/assignments/{id} -> AssignmentDto with heroes and reports (404)
        group.MapGet("/{id:guid}", (Guid id, DomainService domain) =>
        {
            var assignment = domain.Query(ctx => ctx.Assignments.SingleOrDefault(a => a.Id == id)?.ToDto());
            return assignment is null ? Results.NotFound() : Results.Ok(assignment);
        });
    }
}
