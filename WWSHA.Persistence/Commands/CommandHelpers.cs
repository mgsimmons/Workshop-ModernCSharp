using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>Raised when a command refers to an id that is not in the context. Maps to HTTP 404.</summary>
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
    /// The current local time. Commands read it once, before any change, so a clock failure
    /// cannot leave a half-applied command behind.
    /// </summary>
    public static DateTime Now(IDomainContext context) =>
        context.Clock.GetLocalNow().DateTime;

    /// <summary>
    /// Assigns a hero to an incident and persists every object that the assignment touched.
    /// Domain checks run first, so a rejected assignment writes nothing.
    /// </summary>
    public static void AssignAndSave(
        IDomainRepository repository,
        Incident incident,
        Hero hero,
        DateTime now)
    {
        incident.AssignHero(hero, now);

        repository.Save(hero);
        repository.Save(incident.Assignment!);
        repository.Save(incident);
    }

    /// <summary>
    /// Gives free heroes to waiting incidents, oldest first (by occurrence time, then id), until
    /// either no hero is free or no incident is waiting. Called whenever a hero becomes free or
    /// a new incident is filed, so a waiting incident never stays Opened while a hero is idle.
    /// </summary>
    public static void DispatchQueuedIncidents(
        IDomainContext context,
        IDomainRepository repository,
        DateTime now)
    {
        var freeHeroes = new Queue<Hero>(
            context.Heroes.Where(h => h.Availability == Availability.Available));
        if (freeHeroes.Count == 0)
        {
            return;
        }

        var waiting = context.Incidents
            .Where(i => i.Status == IncidentStatus.Opened)
            .OrderBy(i => i.OccurredAt)
            .ThenBy(i => i.Id);

        foreach (var incident in waiting)
        {
            if (freeHeroes.Count == 0)
            {
                return;
            }

            AssignAndSave(repository, incident, freeHeroes.Dequeue(), now);
        }
    }
}
