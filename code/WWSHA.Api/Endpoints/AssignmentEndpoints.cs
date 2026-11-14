using WWSHA.Api.Contracts;
using WWSHA.Api.Infrastructure;
using WWSHA.Domain;
using WWSHA.Persistence;

namespace WWSHA.Api.Endpoints;

public static class AssignmentEndpoints
{
    public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/assignments");

        group.MapGet("/", GetAssignments)
            .WithName("GetAssignments")
            .WithSummary("Lists assignments, optionally filtered by incident, hero, or open state.")
            .Produces<AssignmentDto[]>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetAssignment)
            .WithName("GetAssignment")
            .WithSummary("Gets an assignment with its heroes and damage reports.")
            .Produces<AssignmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static IResult GetAssignments(
        IDomainContext context,
        Guid? incidentId,
        Guid? heroId,
        bool? open)
    {
        var assignments = context.Assignments.ToArray().AsEnumerable();

        if (incidentId is { } incidentFilter)
        {
            assignments = assignments.Where(
                assignment => assignment.IncidentId == incidentFilter);
        }

        if (heroId is { } heroFilter)
        {
            assignments = assignments.Where(
                assignment => assignment.HeroIds.Contains(heroFilter));
        }

        if (open is { } isOpen)
        {
            assignments = assignments.Where(assignment => assignment.IsClosed != isOpen);
        }

        return TypedResults.Ok(
            assignments.Select(assignment => Map(context, assignment)).ToArray());
    }

    private static IResult GetAssignment(Guid id, IDomainContext context)
    {
        var assignment = context.Assignments.SingleOrDefault(candidate => candidate.Id == id)
            ?? throw new EntityNotFoundException(nameof(Assignment), id);

        return TypedResults.Ok(Map(context, assignment));
    }

    private static AssignmentDto Map(IDomainContext context, Assignment assignment) =>
        DtoMapper.ToDto(
            assignment,
            context.HeroesForAssignment(assignment),
            context.ReportsForAssignment(assignment));
}
