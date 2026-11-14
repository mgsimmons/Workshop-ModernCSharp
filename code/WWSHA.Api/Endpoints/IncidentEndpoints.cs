using WWSHA.Api.Contracts;
using WWSHA.Api.Infrastructure;
using WWSHA.Domain;
using WWSHA.Persistence;
using WWSHA.Persistence.Commands;

namespace WWSHA.Api.Endpoints;

public static class IncidentEndpoints
{
    public static IEndpointRouteBuilder MapIncidentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/incidents");

        group.MapPost("/", FileIncidentAsync)
            .WithName("FileIncident")
            .WithSummary("Files a new incident.")
            .Produces<IncidentDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetIncidents)
            .WithName("GetIncidents")
            .WithSummary("Lists incidents, optionally filtered by status, category, or city.")
            .Produces<IncidentDto[]>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetIncident)
            .WithName("GetIncident")
            .WithSummary("Gets an incident with its assignment and reports.")
            .Produces<IncidentDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/heroes", AssignHeroAsync)
            .WithName("AssignHero")
            .WithSummary("Assigns an available hero to an incident.")
            .Produces<AssignmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}/assignment", GetIncidentAssignment)
            .WithName("GetIncidentAssignment")
            .WithSummary("Gets the assignment for an incident.")
            .Produces<AssignmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/reports", FileReportAsync)
            .WithName("FileIncidentReport")
            .WithSummary("Files a collateral-damage report against an incident.")
            .Produces<ReportDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}/reports", GetIncidentReports)
            .WithName("GetIncidentReports")
            .WithSummary("Lists the reports filed against an incident.")
            .Produces<ReportDto[]>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/close", CloseIncidentAsync)
            .WithName("CloseIncident")
            .WithSummary("Closes an incident, assesses assigned heroes, and frees them.")
            .Produces<CloseIncidentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> FileIncidentAsync(
        FileIncidentRequest request,
        IDomainRepository repository,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.City))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["city"] = ["An incident city is required."]
            });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["description"] = ["An incident description is required."]
            });
        }

        if (!Enum.IsDefined(request.Category))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["category"] = ["A valid incident category is required."]
            });
        }

        var incident = await repository.ExecuteAsync(
            new FileIncidentCommand(
                request.City,
                request.OccurredAt,
                request.Description,
                request.Category),
            cancellationToken);

        var assignment = context.FindAssignmentForIncident(incident.Id);

        return TypedResults.Created(
            $"/api/incidents/{incident.Id}",
            DtoMapper.ToDto(incident, assignment));
    }

    private static IResult GetIncidents(
        IDomainContext context,
        string? status,
        string? category,
        string? city)
    {
        IncidentStatus? statusFilter = null;
        if (status is not null)
        {
            if (!EnumParsing.TryParseDefined<IncidentStatus>(status, out var parsedStatus))
            {
                return InvalidFilter("status", status, "incident status");
            }

            statusFilter = parsedStatus;
        }

        IncidentCategory? categoryFilter = null;
        if (category is not null)
        {
            if (!EnumParsing.TryParseDefined<IncidentCategory>(category, out var parsedCategory))
            {
                return InvalidFilter("category", category, "incident category");
            }

            categoryFilter = parsedCategory;
        }

        var incidents = context.Incidents.ToArray().AsEnumerable();

        if (statusFilter is { } statusValue)
        {
            incidents = incidents.Where(incident => incident.Status == statusValue);
        }

        if (categoryFilter is { } categoryValue)
        {
            incidents = incidents.Where(incident => incident.Category == categoryValue);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            incidents = incidents.Where(
                incident => incident.City.Contains(city, StringComparison.OrdinalIgnoreCase));
        }

        var assignments = context.Assignments.ToDictionary(
            assignment => assignment.IncidentId);

        return TypedResults.Ok(
            incidents
                .Select(incident => DtoMapper.ToDto(
                    incident,
                    assignments.GetValueOrDefault(incident.Id)))
                .ToArray());
    }

    private static IResult GetIncident(Guid id, IDomainContext context)
    {
        var incident = context.Incidents.SingleOrDefault(candidate => candidate.Id == id)
            ?? throw new EntityNotFoundException(nameof(Incident), id);

        var assignment = context.FindAssignmentForIncident(incident.Id);
        var reports = context.ReportsForIncident(incident);
        Hero[] heroes = assignment is null
            ? []
            : context.HeroesForAssignment(assignment);

        return TypedResults.Ok(
            DtoMapper.ToDetailDto(incident, assignment, reports, heroes));
    }

    private static async Task<IResult> AssignHeroAsync(
        Guid id,
        AssignHeroRequest request,
        IDomainRepository repository,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var assignment = await repository.ExecuteAsync(
            new AssignHeroCommand(id, request.HeroId),
            cancellationToken);

        return TypedResults.Ok(
            DtoMapper.ToDto(
                assignment,
                context.HeroesForAssignment(assignment),
                context.ReportsForAssignment(assignment)));
    }

    private static IResult GetIncidentAssignment(Guid id, IDomainContext context)
    {
        var assignment = context.FindAssignmentForIncident(id)
            ?? throw new EntityNotFoundException(nameof(Assignment), id);

        return TypedResults.Ok(
            DtoMapper.ToDto(
                assignment,
                context.HeroesForAssignment(assignment),
                context.ReportsForAssignment(assignment)));
    }

    private static async Task<IResult> FileReportAsync(
        Guid id,
        FileReportRequest request,
        IDomainRepository repository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["description"] = ["A report description is required."]
            });
        }

        if (!Enum.IsDefined(request.Severity))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["severity"] = ["A valid report severity is required."]
            });
        }

        var report = await repository.ExecuteAsync(
            new FileDamageReportCommand(
                id,
                request.Description,
                request.Severity,
                request.Cost,
                request.Injury,
                request.ReportedAt),
            cancellationToken);

        return TypedResults.Created($"/api/reports/{report.Id}", DtoMapper.ToDto(report));
    }

    private static IResult GetIncidentReports(Guid id, IDomainContext context)
    {
        var incident = context.Incidents.SingleOrDefault(candidate => candidate.Id == id)
            ?? throw new EntityNotFoundException(nameof(Incident), id);

        return TypedResults.Ok(
            context.ReportsForIncident(incident).Select(DtoMapper.ToDto).ToArray());
    }

    private static async Task<IResult> CloseIncidentAsync(
        Guid id,
        IDomainRepository repository,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var result = await repository.ExecuteAsync(
            new CloseIncidentCommand(id),
            cancellationToken);

        var incident = result.Incident;
        var assignment = context.FindAssignmentForIncident(incident.Id);
        var reports = context.ReportsForIncident(incident);
        Hero[] heroes = assignment is null
            ? []
            : context.HeroesForAssignment(assignment);

        return TypedResults.Ok(new CloseIncidentResponse(
            DtoMapper.ToDetailDto(incident, assignment, reports, heroes),
            [.. result.Assessments.Select(DtoMapper.ToDto)]));
    }

    private static IResult InvalidFilter(string name, string value, string description) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [name] = [$"'{value}' is not a valid {description}."]
        });
}
