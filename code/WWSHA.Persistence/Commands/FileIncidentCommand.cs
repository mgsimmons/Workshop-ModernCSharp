using WWSHA.Domain;
using WWSHA.Persistence.Internal;

namespace WWSHA.Persistence.Commands;

public sealed record FileIncidentCommand(
    string City,
    DateTime OccurredAt,
    string Description,
    IncidentCategory Category) : ICommand<Incident>
{
    public Incident Execute(IDomainRepository repository, IDomainContext context)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);

        var incident = Incident.File(City, OccurredAt, Description, Category);
        repository.AddIncident(incident);
        HeroDispatchService.TryAssignFirstAvailableHero(repository, context, incident);

        return incident;
    }
}
