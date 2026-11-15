using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>
/// Registers a new hero with the agency. The hero starts out available, so any incident still
/// waiting for a hero is dispatched to them right away.
/// </summary>
public sealed record RegisterHeroCommand(string Name, string Superpower, int Reputation = 0)
    : ICommand<Hero>
{
    public Hero Execute(IDomainContext context, IDomainRepository repository)
    {
        var hero = new Hero(Name, Superpower, Reputation);
        repository.Save(hero);

        CommandHelpers.DispatchQueuedIncidents(context, repository);

        return hero;
    }
}
