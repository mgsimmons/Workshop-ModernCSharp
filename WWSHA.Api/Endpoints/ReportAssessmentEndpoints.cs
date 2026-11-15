using WWSHA.Api.Contracts;
using WWSHA.Api.Services;

namespace WWSHA.Api.Endpoints;

public static class ReportAssessmentEndpoints
{
    public static void MapReportAndAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/reports").WithTags("Reports");

        // GET /api/reports/{id} -> ReportDto (404)
        reports.MapGet("/{id:guid}", (Guid id, DomainService domain) =>
        {
            var report = domain.Query(ctx => ctx.Reports.SingleOrDefault(r => r.Id == id)?.ToDto());
            return report is null ? Results.NotFound() : Results.Ok(report);
        });

        var assessments = app.MapGroup("/api/assessments").WithTags("Assessments");

        // GET /api/assessments?heroId=&incidentId= -> AssessmentDto[]
        assessments.MapGet("/", (Guid? heroId, Guid? incidentId, DomainService domain) =>
        {
            var results = domain.Query(ctx => ctx.Assessments
                .Where(a => heroId is null || a.HeroId == heroId)
                .Where(a => incidentId is null || a.IncidentId == incidentId)
                .Select(a => a.ToDto())
                .ToArray());
            return Results.Ok(results);
        });
    }
}
