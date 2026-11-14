using WWSHA.Api.Contracts;
using WWSHA.Domain;

namespace WWSHA.Api.Infrastructure;

/// <summary>
/// Maps domain entities to API contracts. Endpoints never return domain objects directly.
/// </summary>
public static class DtoMapper
{
    public static HeroDto ToDto(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);

        return new HeroDto(
            hero.Id,
            hero.Name,
            hero.Superpower,
            hero.Availability.ToString(),
            hero.Reputation,
            hero.CurrentIncidentId,
            hero.Assessments.Count);
    }

    public static HeroDetailDto ToDetailDto(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);

        return new HeroDetailDto(
            hero.Id,
            hero.Name,
            hero.Superpower,
            hero.Availability.ToString(),
            hero.Reputation,
            hero.CurrentIncidentId,
            [.. hero.Assessments.Select(ToDto)]);
    }

    public static AssessmentDto ToDto(Assessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        return new AssessmentDto(
            assessment.Id,
            assessment.HeroId,
            assessment.IncidentId,
            assessment.DamageReportCount,
            assessment.ReputationDelta,
            assessment.CreatedAt);
    }

    public static IncidentDto ToDto(Incident incident, Assignment? assignment = null)
    {
        ArgumentNullException.ThrowIfNull(incident);

        return new IncidentDto(
            incident.Id,
            incident.City,
            incident.OccurredAt,
            incident.Description,
            incident.Category.ToString(),
            incident.Status.ToString(),
            incident.AssignmentId,
            assignment?.HeroIds.Count ?? 0,
            incident.ReportIds.Count);
    }

    public static IncidentDetailDto ToDetailDto(
        Incident incident,
        Assignment? assignment,
        IEnumerable<Report> reports,
        IEnumerable<Hero> heroes)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(heroes);

        return new IncidentDetailDto(
            incident.Id,
            incident.City,
            incident.OccurredAt,
            incident.Description,
            incident.Category.ToString(),
            incident.Status.ToString(),
            incident.AssignmentId,
            assignment?.HeroIds.Count ?? 0,
            incident.ReportIds.Count,
            assignment is null ? null : ToDto(assignment, heroes, reports),
            [.. reports.Select(ToDto)]);
    }

    public static ReportDto ToDto(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new ReportDto(
            report.Id,
            report.IncidentId,
            report.AssignmentId,
            report.City,
            report.Description,
            report.Severity,
            report.Cost,
            report.Injury,
            report.ReportedAt);
    }

    public static AssignmentDto ToDto(
        Assignment assignment,
        IEnumerable<Hero> heroes,
        IEnumerable<Report> reports)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(heroes);
        ArgumentNullException.ThrowIfNull(reports);

        var heroLookup = heroes.ToDictionary(hero => hero.Id);
        var reportLookup = reports.ToDictionary(report => report.Id);

        return new AssignmentDto(
            assignment.Id,
            assignment.IncidentId,
            assignment.CreatedAt,
            assignment.ClosedAt,
            [.. assignment.HeroIds.Where(heroLookup.ContainsKey).Select(id => ToDto(heroLookup[id]))],
            [.. assignment.DamageReportIds.Where(reportLookup.ContainsKey).Select(id => ToDto(reportLookup[id]))]);
    }
}
