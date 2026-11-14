using WWSHA.Domain;

namespace WWSHA.Persistence;

/// <summary>
/// The read-only view of the in-memory store. Query properties are LINQ-to-Objects snapshots over
/// the in-memory collections; materialize a query before relying on it if the store may be mutated
/// concurrently. <see cref="Assessments"/> is derived from <see cref="Hero.Assessments"/> across
/// every hero.
/// </summary>
public interface IDomainContext
{
    IQueryable<Hero> Heroes { get; }
    IQueryable<Incident> Incidents { get; }
    IQueryable<Assignment> Assignments { get; }
    IQueryable<Report> Reports { get; }
    IQueryable<Assessment> Assessments { get; }

    Hero GetHero(Guid id) =>
        Heroes.SingleOrDefault(hero => hero.Id == id)
        ?? throw new EntityNotFoundException(nameof(Hero), id);

    Incident GetIncident(Guid id) =>
        Incidents.SingleOrDefault(incident => incident.Id == id)
        ?? throw new EntityNotFoundException(nameof(Incident), id);

    Assignment GetAssignment(Guid id) =>
        Assignments.SingleOrDefault(assignment => assignment.Id == id)
        ?? throw new EntityNotFoundException(nameof(Assignment), id);

    Report GetReport(Guid id) =>
        Reports.SingleOrDefault(report => report.Id == id)
        ?? throw new EntityNotFoundException(nameof(Report), id);

    Assessment GetAssessment(Guid id) =>
        Assessments.SingleOrDefault(assessment => assessment.Id == id)
        ?? throw new EntityNotFoundException(nameof(Assessment), id);

    Assignment? FindAssignmentForIncident(Guid incidentId) =>
        Assignments.SingleOrDefault(assignment => assignment.IncidentId == incidentId);

    IQueryable<Report> ReportsForIncident(Guid incidentId) =>
        Reports.Where(report => report.IncidentId == incidentId);

    IQueryable<Report> PendingReportsForIncident(Guid incidentId) =>
        Reports.Where(report => report.IncidentId == incidentId && report.AssignmentId == null);
}
