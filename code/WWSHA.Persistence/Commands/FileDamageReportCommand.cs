using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

public sealed record FileDamageReportCommand(
    Guid IncidentId,
    string Description,
    ReportSeverity Severity,
    int Cost,
    int Injury,
    DateTime? ReportedAt = null) : ICommand<Report>
{
    public Report Execute(IDomainRepository repository, IDomainContext context)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);

        var incident = context.GetIncident(IncidentId);
        if (incident.IsClosed)
        {
            throw new IncidentClosedException(incident.Id);
        }

        var report = Report.File(
            incident.Id,
            incident.City,
            Description,
            Severity,
            Cost,
            Injury,
            ReportedAt ?? DateTime.Now);

        repository.AddReport(report);
        incident.AddReport(report.Id);

        var assignment = context.FindAssignmentForIncident(incident.Id);
        if (assignment is not null)
        {
            assignment.AddDamageReport(report.Id);
            report.AttachToAssignment(assignment.Id);
        }

        return report;
    }
}
