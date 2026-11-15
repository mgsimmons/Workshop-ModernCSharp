using WWSHA.Domain;

namespace WWSHA.Api.Contracts;

/// <summary>
/// Maps domain objects to DTOs. Call these only inside <c>DomainService.Query</c> or
/// <c>DomainService.Command</c>, so the objects are read while the store lock is held.
/// </summary>
public static class Mappers
{
    public static HeroDto ToDto(this Hero hero) =>
        new(hero.Id, hero.Name, hero.Superpower, hero.Availability, hero.Reputation);

    public static HeroDetailDto ToDetailDto(this Hero hero) =>
        new(hero.Id, hero.Name, hero.Superpower, hero.Availability, hero.Reputation,
            hero.Assessments.Select(a => a.ToDto()).ToArray());

    public static IncidentDto ToDto(this Incident incident) =>
        new(incident.Id, incident.City, incident.OccurredAt, incident.Description,
            incident.Category, incident.Status, incident.Assignment?.Id);

    public static AssignmentDto ToDto(this Assignment assignment) =>
        new(assignment.Id,
            assignment.Incident.Id,
            assignment.Incident.Status,
            assignment.CreatedAt,
            assignment.ClosedAt,
            assignment.Heroes.Select(h => h.ToDto()).ToArray(),
            assignment.Reports.Select(r => r.ToDto()).ToArray());

    public static ReportDto ToDto(this Report report) =>
        new(report.Id, report.IncidentId, report.City, report.Description,
            report.Severity, report.Cost, report.Injury, report.FiledAt);

    public static AssessmentDto ToDto(this Assessment assessment) =>
        new(assessment.Id, assessment.HeroId, assessment.IncidentId,
            assessment.ReportCount, assessment.ReputationChange, assessment.CreatedAt);

    public static IncidentDetailDto ToDetailDto(this Incident incident) =>
        new(incident.ToDto(),
            incident.Assignment?.ToDto(),
            incident.Reports.Select(r => r.ToDto()).ToArray());
}
