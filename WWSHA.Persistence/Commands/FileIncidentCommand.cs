using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>
/// Files a new incident. It is queued like any other Opened incident, and free heroes are dispatched
/// oldest-first. If no hero is free it stays Opened until one becomes available. An incident cannot
/// occur more than a day in the future.
/// </summary>
public sealed record FileIncidentCommand(
    string City,
    DateTime OccurredAt,
    string Description,
    IncidentCategory Category) : ICommand<Incident>
{
    /// <summary>How far ahead of the clock an incident's occurrence time may be, to allow for clock skew.</summary>
    public static readonly TimeSpan FutureTolerance = TimeSpan.FromDays(1);

    public Incident Execute(IDomainContext context, IDomainRepository repository)
    {
        var now = CommandHelpers.Now(context);

        if (OccurredAt > now + FutureTolerance)
            throw new InvalidIncidentException(
                $"An incident cannot occur more than {FutureTolerance.TotalDays} day(s) in the future.");

        var incident = new Incident(City, OccurredAt, Description, Category);
        repository.Save(incident);

        CommandHelpers.DispatchQueuedIncidents(context, repository, now);

        return incident;
    }
}
