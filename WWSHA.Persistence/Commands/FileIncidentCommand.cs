using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>
/// Files a new incident. If a hero is available, the first available one (in registration order)
/// is assigned straight away and the incident becomes InProgress. Otherwise it stays Opened.
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

        var available = context.Heroes.FirstOrDefault(h => h.Availability == Availability.Available);
        if (available is not null)
            CommandHelpers.AssignAndSave(incident, available, repository);

        return incident;
    }
}
