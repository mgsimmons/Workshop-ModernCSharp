using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>Adds an available hero to an incident's assignment (up to three heroes total).</summary>
public sealed record AssignHeroCommand(Guid IncidentId, Guid HeroId) : ICommand<Assignment>
{
    public Assignment Execute(IDomainContext context, IDomainRepository repository)
    {
        var incident = CommandHelpers.FindIncident(context, IncidentId);
        var hero = CommandHelpers.FindHero(context, HeroId);

        CommandHelpers.AssignAndSave(context, repository, incident, hero);

        return incident.Assignment!;
    }
}
