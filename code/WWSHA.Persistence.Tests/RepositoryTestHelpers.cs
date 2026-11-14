using WWSHA.Domain;
using WWSHA.Persistence.Commands;

namespace WWSHA.Persistence.Tests;

internal static class RepositoryTestHelpers
{
    internal static Task<Hero> RegisterHeroAsync(
        IDomainRepository repository,
        string name = "The Sloth",
        string superpower = "Extreme procrastination") =>
        repository.ExecuteAsync(new RegisterHeroCommand(name, superpower));

    internal static Task<Incident> FileIncidentAsync(
        IDomainRepository repository,
        DateTime? occurredAt = null,
        string city = "Townsville",
        IncidentCategory category = IncidentCategory.PettyCrime) =>
        repository.ExecuteAsync(new FileIncidentCommand(
            city,
            occurredAt ?? DateTime.Now,
            "A very slow chase",
            category));

    internal static Task<Report> FileReportAsync(
        IDomainRepository repository,
        Guid incidentId,
        ReportSeverity severity = ReportSeverity.Minor,
        int cost = 100,
        int injury = 0,
        DateTime? reportedAt = null) =>
        repository.ExecuteAsync(new FileDamageReportCommand(
            incidentId,
            "A parked car was mildly inconvenienced",
            severity,
            cost,
            injury,
            reportedAt));
}
