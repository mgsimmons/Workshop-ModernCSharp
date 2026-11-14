using WWSHA.Domain;
using WWSHA.Persistence;

namespace WWSHA.Api.Infrastructure;

/// <summary>
/// Convenience projections over <see cref="IDomainContext"/> query properties.
/// </summary>
internal static class RepositoryLookups
{
    internal static Assignment? FindAssignmentForIncident(
        this IDomainContext context,
        Guid incidentId)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Assignments.SingleOrDefault(
            assignment => assignment.IncidentId == incidentId);
    }

    internal static Hero[] HeroesForAssignment(
        this IDomainContext context,
        Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(assignment);

        var lookup = context.Heroes.ToDictionary(hero => hero.Id);

        return [.. assignment.HeroIds.Where(lookup.ContainsKey).Select(id => lookup[id])];
    }

    internal static Report[] ReportsForAssignment(
        this IDomainContext context,
        Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(assignment);

        var lookup = context.Reports.ToDictionary(report => report.Id);

        return [.. assignment.DamageReportIds.Where(lookup.ContainsKey).Select(id => lookup[id])];
    }

    internal static Report[] ReportsForIncident(
        this IDomainContext context,
        Incident incident)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(incident);

        var lookup = context.Reports.ToDictionary(report => report.Id);

        return [.. incident.ReportIds.Where(lookup.ContainsKey).Select(id => lookup[id])];
    }
}
