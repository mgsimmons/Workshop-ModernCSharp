using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>The outcome of closing an incident: the closed incident and one assessment per hero.</summary>
public sealed record CloseIncidentResult(Incident Incident, IReadOnlyList<Assessment> Assessments);

/// <summary>
/// Closes an in-progress incident, assesses each assigned hero, frees them, and then dispatches
/// the freed heroes to any incidents still waiting, oldest first.
/// </summary>
public sealed record CloseIncidentCommand(Guid IncidentId) : ICommand<CloseIncidentResult>
{
    public CloseIncidentResult Execute(IDomainContext context, IDomainRepository repository)
    {
        var incident = CommandHelpers.FindIncident(context, IncidentId);

        incident.Close(CommandHelpers.Now(context));

        var heroes = incident.Assignment!.Heroes;
        var assessments = heroes
            .Select(hero => hero.Assessments.Last(a => a.IncidentId == incident.Id))
            .ToList();

        foreach (var hero in heroes)
            repository.Save(hero);
        foreach (var assessment in assessments)
            repository.Save(assessment);
        repository.Save(incident.Assignment!);
        repository.Save(incident);

        CommandHelpers.DispatchQueuedIncidents(context, repository);

        return new CloseIncidentResult(incident, assessments);
    }
}
