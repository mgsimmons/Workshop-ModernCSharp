using WWSHA.Domain;
using WWSHA.Persistence.Internal;

namespace WWSHA.Persistence.Commands;

public sealed record AssignHeroCommand(Guid IncidentId, Guid? HeroId = null) : ICommand<Assignment>
{
    public Assignment Execute(IDomainRepository repository, IDomainContext context)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);

        var incident = context.GetIncident(IncidentId);
        if (incident.IsClosed)
        {
            throw new IncidentClosedException(incident.Id);
        }

        var hero = HeroId is { } heroId
            ? context.GetHero(heroId)
            : context.Heroes.FirstOrDefault(
                candidate => candidate.Availability == HeroAvailability.Available)
                ?? throw new HeroUnavailableException(Guid.Empty);

        return HeroDispatchService.AssignHero(repository, context, incident, hero);
    }
}
