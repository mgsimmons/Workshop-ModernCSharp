using WWSHA.Domain;

namespace WWSHA.Persistence.Commands;

/// <summary>Files a damage report against an in-progress incident.</summary>
public sealed record FileDamageReportCommand(
    Guid IncidentId,
    string City,
    string Description,
    DateTime FiledAt) : ICommand<Report>
{
    public Report Execute(IDomainContext context, IDomainRepository repository)
    {
        var incident = CommandHelpers.FindIncident(context, IncidentId);

        var report = incident.FileReport(City, Description, FiledAt);

        repository.Save(report);
        repository.Save(incident.Assignment!);

        return report;
    }
}
