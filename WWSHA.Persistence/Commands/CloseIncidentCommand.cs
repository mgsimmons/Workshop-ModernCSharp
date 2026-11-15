using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>The outcome of closing an incident: the closed incident and one assessment per hero.</summary>
public sealed record CloseIncidentResult(Incident Incident, IReadOnlyList<Assessment> Assessments);

/// <summary>Closes an in-progress incident, assesses each assigned hero, and frees them for new work.</summary>
public sealed record CloseIncidentCommand(Guid IncidentId) : ICommand<CloseIncidentResult>
{
    public CloseIncidentResult Execute(IDomainContext context, IDomainRepository repository)
    {
        var incident = CommandHelpers.FindIncident(context, IncidentId);

        incident.Close();

        var assessments = incident.Assignment!.Heroes
            .Select(hero => hero.Assessments.Last(a => a.IncidentId == incident.Id))
            .ToList();

        foreach (var hero in incident.Assignment.Heroes)
            repository.Save(hero);
        foreach (var assessment in assessments)
            repository.Save(assessment);
        repository.Save(incident);

        return new CloseIncidentResult(incident, assessments);
    }
}
