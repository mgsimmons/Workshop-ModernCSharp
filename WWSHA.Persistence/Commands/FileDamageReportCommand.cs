using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>
/// Files a damage report against an incident that is Opened or InProgress. If <see cref="FiledAt"/>
/// is omitted, the context clock's current time is used.
/// </summary>
public sealed record FileDamageReportCommand(
    Guid IncidentId,
    string City,
    string Description,
    ReportSeverity Severity,
    int Cost,
    int Injury,
    DateTime? FiledAt = null) : ICommand<Report>
{
    public Report Execute(IDomainContext context, IDomainRepository repository)
    {
        var now = CommandHelpers.Now(context);
        var incident = CommandHelpers.FindIncident(context, IncidentId);

        var report = incident.FileReport(City, Description, Severity, Cost, Injury, FiledAt ?? now);

        repository.Save(report);
        if (incident.Assignment is not null)
            repository.Save(incident.Assignment);
        repository.Save(incident);

        return report;
    }
}
