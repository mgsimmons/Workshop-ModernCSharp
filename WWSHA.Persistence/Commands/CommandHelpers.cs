using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>Raised when a command refers to an id that is not in the context. Maps to HTTP 404 later.</summary>
public sealed class EntityNotFoundException(string entityName, Guid id)
    : Exception($"{entityName} '{id}' was not found.");

internal static class CommandHelpers
{
    public static Incident FindIncident(IDomainContext context, Guid id) =>
        context.Incidents.SingleOrDefault(i => i.Id == id)
        ?? throw new EntityNotFoundException(nameof(Incident), id);

    public static Hero FindHero(IDomainContext context, Guid id) =>
        context.Heroes.SingleOrDefault(h => h.Id == id)
        ?? throw new EntityNotFoundException(nameof(Hero), id);

    /// <summary>
    /// Assigns a hero to an incident and persists every object that the assignment touched.
    /// Domain checks run first, so a rejected assignment writes nothing.
    /// </summary>
    public static void AssignAndSave(Incident incident, Hero hero, IDomainRepository repository)
    {
        incident.AssignHero(hero);

        repository.Save(hero);
        repository.Save(incident);
        repository.Save(incident.Assignment!);
    }
}
