using WWSHA.Api.Contracts;
using WWSHA.Api.Infrastructure;
using WWSHA.Domain;
using WWSHA.Persistence;

namespace WWSHA.Api.Endpoints;

public static class ReportAndAssessmentEndpoints
{
    public static IEndpointRouteBuilder MapReportAndAssessmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/reports/{id:guid}", GetReport)
            .WithName("GetReport")
            .WithSummary("Gets a collateral-damage report.")
            .Produces<ReportDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet("/assessments", GetAssessments)
            .WithName("GetAssessments")
            .WithSummary("Lists assessments, optionally filtered by hero or incident.")
            .Produces<AssessmentDto[]>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static IResult GetReport(Guid id, IDomainContext context)
    {
        var report = context.Reports.SingleOrDefault(candidate => candidate.Id == id)
            ?? throw new EntityNotFoundException(nameof(Report), id);

        return TypedResults.Ok(DtoMapper.ToDto(report));
    }

    private static IResult GetAssessments(
        IDomainContext context,
        Guid? heroId,
        Guid? incidentId)
    {
        var assessments = context.Assessments.ToArray().AsEnumerable();

        if (heroId is { } heroFilter)
        {
            assessments = assessments.Where(
                assessment => assessment.HeroId == heroFilter);
        }

        if (incidentId is { } incidentFilter)
        {
            assessments = assessments.Where(
                assessment => assessment.IncidentId == incidentFilter);
        }

        return TypedResults.Ok(assessments.Select(DtoMapper.ToDto).ToArray());
    }
}
