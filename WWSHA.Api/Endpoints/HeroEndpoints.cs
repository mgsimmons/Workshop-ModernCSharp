using WWSHA.Api.Contracts;
using WWSHA.Api.Services;
using WWSHA.Domain;
using WWSHA.Persistence.Commands;

namespace WWSHA.Api.Endpoints;

public static class HeroEndpoints
{
    public static void MapHeroEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/heroes").WithTags("Heroes");

        // POST /api/heroes -> RegisterHeroCommand (201 + HeroDto, 400)
        group.MapPost("/", (RegisterHeroRequest request, DomainService domain) =>
        {
            var hero = domain.Command(
                new RegisterHeroCommand(request.Name, request.Superpower, request.Reputation),
                h => h.ToDto());
            return Results.Created($"/api/heroes/{hero.Id}", hero);
        });

        // GET /api/heroes?available={bool?} -> HeroDto[]
        group.MapGet("/", (bool? available, DomainService domain) =>
        {
            var heroes = domain.Query(ctx => ctx.Heroes
                .Where(h => available is null
                    || (h.Availability == Availability.Available) == available)
                .Select(h => h.ToDto())
                .ToArray());
            return Results.Ok(heroes);
        });

        // GET /api/heroes/{id} -> HeroDetailDto (404)
        group.MapGet("/{id:guid}", (Guid id, DomainService domain) =>
        {
            var hero = domain.Query(ctx => ctx.Heroes.SingleOrDefault(h => h.Id == id)?.ToDetailDto());
            return hero is null ? Results.NotFound() : Results.Ok(hero);
        });

        // GET /api/heroes/{id}/assessments -> AssessmentDto[] (404)
        group.MapGet("/{id:guid}/assessments", (Guid id, DomainService domain) =>
        {
            var assessments = domain.Query(ctx =>
                ctx.Heroes.SingleOrDefault(h => h.Id == id)
                    ?.Assessments.Select(a => a.ToDto()).ToArray());
            return assessments is null ? Results.NotFound() : Results.Ok(assessments);
        });
    }
}
