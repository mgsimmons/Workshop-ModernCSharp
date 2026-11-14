using WWSHA.Api.Contracts;
using WWSHA.Api.Infrastructure;
using WWSHA.Domain;
using WWSHA.Persistence;
using WWSHA.Persistence.Commands;

namespace WWSHA.Api.Endpoints;

public static class HeroEndpoints
{
    public static IEndpointRouteBuilder MapHeroEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/heroes");

        group.MapPost("/", RegisterHeroAsync)
            .WithName("RegisterHero")
            .WithSummary("Registers a new hero.")
            .Produces<HeroDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetHeroes)
            .WithName("GetHeroes")
            .WithSummary("Lists heroes, optionally filtered by availability.")
            .Produces<HeroDto[]>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetHero)
            .WithName("GetHero")
            .WithSummary("Gets a hero and their assessment history.")
            .Produces<HeroDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/assessments", GetHeroAssessments)
            .WithName("GetHeroAssessments")
            .WithSummary("Lists the assessments recorded for a hero.")
            .Produces<AssessmentDto[]>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> RegisterHeroAsync(
        RegisterHeroRequest request,
        IDomainRepository repository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["A hero name is required."]
            });
        }

        if (string.IsNullOrWhiteSpace(request.Superpower))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["superpower"] = ["A hero superpower is required."]
            });
        }

        var hero = await repository.ExecuteAsync(
            new RegisterHeroCommand(request.Name, request.Superpower),
            cancellationToken);

        return TypedResults.Created($"/api/heroes/{hero.Id}", DtoMapper.ToDto(hero));
    }

    private static IResult GetHeroes(IDomainContext context, bool? available)
    {
        var heroes = context.Heroes.ToArray().AsEnumerable();

        if (available is { } isAvailable)
        {
            heroes = heroes.Where(
                hero => (hero.Availability == HeroAvailability.Available) == isAvailable);
        }

        return TypedResults.Ok(heroes.Select(DtoMapper.ToDto).ToArray());
    }

    private static IResult GetHero(Guid id, IDomainContext context)
    {
        var hero = context.Heroes.SingleOrDefault(candidate => candidate.Id == id)
            ?? throw new EntityNotFoundException(nameof(Hero), id);

        return TypedResults.Ok(DtoMapper.ToDetailDto(hero));
    }

    private static IResult GetHeroAssessments(Guid id, IDomainContext context)
    {
        _ = context.Heroes.SingleOrDefault(candidate => candidate.Id == id)
            ?? throw new EntityNotFoundException(nameof(Hero), id);

        var assessments = context.Assessments
            .Where(assessment => assessment.HeroId == id)
            .ToArray()
            .Select(DtoMapper.ToDto)
            .ToArray();

        return TypedResults.Ok(assessments);
    }
}
