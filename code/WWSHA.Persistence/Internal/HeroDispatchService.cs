using WWSHA.Domain;

namespace WWSHA.Persistence.Internal;

/// <summary>
/// Shared assignment and auto-dispatch rules. Commands delegate here so the rules are
/// implemented once.
/// </summary>
internal static class HeroDispatchService
{
    public static Assignment AssignHero(
        IDomainRepository repository,
        IDomainContext context,
        Incident incident,
        Hero hero)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(hero);

        if (incident.IsClosed)
        {
            throw new IncidentClosedException(incident.Id);
        }

        if (hero.Availability != HeroAvailability.Available)
        {
            throw new HeroUnavailableException(hero.Id);
        }

        var assignment = context.FindAssignmentForIncident(incident.Id);
        if (assignment is null)
        {
            assignment = Assignment.Create(incident.Id, DateTime.Now);
            repository.AddAssignment(assignment);
        }

        assignment.AddHero(hero.Id);
        hero.AssignTo(incident.Id);

        if (incident.AssignmentId is null)
        {
            incident.AttachAssignment(assignment.Id);
        }

        AttachReports(context, incident, assignment);

        return assignment;
    }

    public static bool TryAssignFirstAvailableHero(
        IDomainRepository repository,
        IDomainContext context,
        Incident incident)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(incident);

        if (incident.Status != IncidentStatus.Opened)
        {
            return false;
        }

        var hero = context.Heroes.FirstOrDefault(
            candidate => candidate.Availability == HeroAvailability.Available);

        if (hero is null)
        {
            return false;
        }

        AssignHero(repository, context, incident, hero);
        return true;
    }

    public static void DispatchAvailableHeroes(IDomainRepository repository, IDomainContext context)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);

        var openIncidents = context.Incidents
            .Where(incident => incident.Status == IncidentStatus.Opened)
            .OrderBy(incident => incident.OccurredAt)
            .ThenBy(incident => incident.Id)
            .ToArray();

        foreach (var incident in openIncidents)
        {
            if (!TryAssignFirstAvailableHero(repository, context, incident))
            {
                break;
            }
        }
    }

    public static void AttachReports(IDomainContext context, Incident incident, Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(assignment);

        foreach (var reportId in incident.ReportIds)
        {
            if (assignment.DamageReportIds.Contains(reportId))
            {
                continue;
            }

            var report = context.GetReport(reportId);
            report.AttachToAssignment(assignment.Id);
            assignment.AddDamageReport(report.Id);
        }
    }
}
