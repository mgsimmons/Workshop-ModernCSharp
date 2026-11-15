using WWSHA.Api.Contracts;
using WWSHA.Api.Services;
using WWSHA.Domain;
using WWSHA.Persistence.Commands;

namespace WWSHA.Api.Endpoints;

public static class IncidentEndpoints
{
    public static void MapIncidentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/incidents").WithTags("Incidents");

        // POST /api/incidents -> FileIncidentCommand (201 + IncidentDto, 400)
        group.MapPost("/", (FileIncidentRequest request, DomainService domain) =>
        {
            var incident = domain.Command(
                new FileIncidentCommand(request.City, request.OccurredAt, request.Description, request.Category),
                i => i.ToDto());
            return Results.Created($"/api/incidents/{incident.Id}", incident);
        });

        // GET /api/incidents?status=&category=&city= -> IncidentDto[] (400 on invalid enum)
        group.MapGet("/", (string? status, string? category, string? city, DomainService domain) =>
        {
            IncidentStatus? parsedStatus = null;
            if (status is not null)
            {
                if (!TryParseEnum(status, out IncidentStatus s))
                    return Results.Problem(statusCode: 400, title: "Invalid request",
                        detail: $"'{status}' is not a valid incident status.");
                parsedStatus = s;
            }

            IncidentCategory? parsedCategory = null;
            if (category is not null)
            {
                if (!TryParseEnum(category, out IncidentCategory c))
                    return Results.Problem(statusCode: 400, title: "Invalid request",
                        detail: $"'{category}' is not a valid incident category.");
                parsedCategory = c;
            }

            var incidents = domain.Query(ctx => ctx.Incidents
                .Where(i => parsedStatus is null || i.Status == parsedStatus)
                .Where(i => parsedCategory is null || i.Category == parsedCategory)
                .Where(i => city is null || string.Equals(i.City, city, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.ToDto())
                .ToArray());
            return Results.Ok(incidents);
        });

        // GET /api/incidents/{id} -> IncidentDetailDto (404)
        group.MapGet("/{id:guid}", (Guid id, DomainService domain) =>
        {
            var detail = domain.Query(ctx => ctx.Incidents.SingleOrDefault(i => i.Id == id)?.ToDetailDto());
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        // POST /api/incidents/{id}/heroes -> AssignHeroCommand (200 + AssignmentDto, 404, 409)
        group.MapPost("/{id:guid}/heroes", (Guid id, AssignHeroRequest request, DomainService domain) =>
        {
            var assignment = domain.Command(new AssignHeroCommand(id, request.HeroId), a => a.ToDto());
            return Results.Ok(assignment);
        });

        // GET /api/incidents/{id}/assignment -> AssignmentDto (404)
        group.MapGet("/{id:guid}/assignment", (Guid id, DomainService domain) =>
        {
            var assignment = domain.Query(ctx =>
                ctx.Incidents.SingleOrDefault(i => i.Id == id)?.Assignment?.ToDto());
            return assignment is null ? Results.NotFound() : Results.Ok(assignment);
        });

        // POST /api/incidents/{id}/reports -> FileDamageReportCommand (201 + ReportDto, 400, 404, 409)
        group.MapPost("/{id:guid}/reports", (Guid id, FileReportRequest request, DomainService domain) =>
        {
            var report = domain.Command(
                new FileDamageReportCommand(id, request.City, request.Description,
                    request.Severity, request.Cost, request.Injury, request.FiledAt),
                r => r.ToDto());
            return Results.Created($"/api/reports/{report.Id}", report);
        });

        // GET /api/incidents/{id}/reports -> ReportDto[] (404)
        group.MapGet("/{id:guid}/reports", (Guid id, DomainService domain) =>
        {
            var reports = domain.Query<ReportDto[]?>(ctx =>
                ctx.Incidents.SingleOrDefault(i => i.Id == id)
                    ?.Reports.Select(r => r.ToDto()).ToArray());
            return reports is null ? Results.NotFound() : Results.Ok(reports);
        });

        // POST /api/incidents/{id}/close -> CloseIncidentCommand (200 + CloseIncidentResponse, 404, 409)
        group.MapPost("/{id:guid}/close", (Guid id, DomainService domain) =>
        {
            var response = domain.Command(
                new CloseIncidentCommand(id),
                r => new CloseIncidentResponse(
                    r.Incident.ToDto(),
                    r.Assessments.Select(a => a.ToDto()).ToArray()));
            return Results.Ok(response);
        });
    }

    /// <summary>Parses an enum name ignoring case, rejecting numeric strings and unknown names.</summary>
    internal static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct, Enum
    {
        if (Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result))
            return true;

        result = default;
        return false;
    }
}
