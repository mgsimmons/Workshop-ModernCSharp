using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>Adds an available hero to an incident's assignment (up to three heroes total).</summary>
public sealed record AssignHeroCommand(Guid IncidentId, Guid HeroId) : ICommand<Assignment>
{
    public Assignment Execute(IDomainContext context, IDomainRepository repository)
    {
        var now = CommandHelpers.Now(context);
        var incident = CommandHelpers.FindIncident(context, IncidentId);
        var hero = CommandHelpers.FindHero(context, HeroId);

        CommandHelpers.AssignAndSave(repository, incident, hero, now);

        return incident.Assignment!;
    }
}
