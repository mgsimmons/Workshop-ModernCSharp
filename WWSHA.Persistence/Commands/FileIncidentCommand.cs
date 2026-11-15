using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>
/// Files a new incident. It is queued like any other Opened incident, and free heroes are dispatched
/// oldest-first. If no hero is free it stays Opened until one becomes available.
/// </summary>
public sealed record FileIncidentCommand(
    string City,
    DateTime OccurredAt,
    string Description,
    IncidentCategory Category) : ICommand<Incident>
{
    public Incident Execute(IDomainContext context, IDomainRepository repository)
    {
        var incident = new Incident(City, OccurredAt, Description, Category);
        repository.Save(incident);

        CommandHelpers.DispatchQueuedIncidents(context, repository);

        return incident;
    }
}
