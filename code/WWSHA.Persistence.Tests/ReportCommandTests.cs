using WWSHA.Domain;
using WWSHA.Persistence.Commands;
using WWSHA.Persistence.InMemory;

namespace WWSHA.Persistence.Tests;

public sealed class ReportCommandTests
{
    [Fact]
    public async Task FileReport_BeforeAssignment_RemainsPendingOnIncident()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        var report = await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);

        Assert.Equal(report.Id, Assert.Single(incident.ReportIds));
        Assert.Null(report.AssignmentId);
        Assert.Empty(repository.Assignments);
        Assert.Same(report, Assert.Single(repository.Reports));
    }

    [Fact]
    public async Task AssigningHero_AttachesPendingReports()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        var report = await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);

        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);

        var assignment = Assert.Single(repository.Assignments);
        Assert.Equal(hero.Id, Assert.Single(assignment.HeroIds));
        Assert.Equal(report.Id, Assert.Single(assignment.DamageReportIds));
        Assert.Equal(assignment.Id, report.AssignmentId);
    }

    [Fact]
    public async Task FileReport_AfterAssignment_IsRecordedImmediately()
    {
        var repository = new InMemoryDomainRepository();
        await RepositoryTestHelpers.RegisterHeroAsync(repository);
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        var report = await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);

        var assignment = Assert.Single(repository.Assignments);
        Assert.Equal(report.Id, Assert.Single(incident.ReportIds));
        Assert.Equal(report.Id, Assert.Single(assignment.DamageReportIds));
        Assert.Equal(assignment.Id, report.AssignmentId);
    }

    [Fact]
    public async Task FileReport_StoresSeverityCostInjuryAndTimestamp()
    {
        var repository = new InMemoryDomainRepository();
        await RepositoryTestHelpers.RegisterHeroAsync(repository);
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        var reportedAt = new DateTime(2026, 11, 11, 10, 30, 0, DateTimeKind.Utc);

        var report = await RepositoryTestHelpers.FileReportAsync(
            repository,
            incident.Id,
            ReportSeverity.Severe,
            1234,
            2,
            reportedAt);

        Assert.Equal(ReportSeverity.Severe, report.Severity);
        Assert.Equal(1234, report.Cost);
        Assert.Equal(2, report.Injury);
        Assert.Equal(reportedAt, report.ReportedAt);
        Assert.Equal(incident.City, report.City);
        Assert.Equal(incident.Id, report.IncidentId);
    }

    [Fact]
    public async Task FileReport_NegativeCost_ThrowsAndLeavesNoReport()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => repository.ExecuteAsync(
                new FileDamageReportCommand(
                    incident.Id,
                    "Bad cost",
                    ReportSeverity.Minor,
                    -1,
                    0)));

        Assert.Empty(repository.Reports);
        Assert.Empty(incident.ReportIds);
    }

    [Fact]
    public async Task FileReport_NegativeInjury_ThrowsAndLeavesNoReport()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => repository.ExecuteAsync(
                new FileDamageReportCommand(
                    incident.Id,
                    "Bad injury",
                    ReportSeverity.Minor,
                    0,
                    -1)));

        Assert.Empty(repository.Reports);
        Assert.Empty(incident.ReportIds);
    }

    [Fact]
    public async Task FileReport_UndefinedSeverity_ThrowsAndLeavesNoReport()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => repository.ExecuteAsync(
                new FileDamageReportCommand(
                    incident.Id,
                    "Bad severity",
                    (ReportSeverity)999,
                    0,
                    0)));

        Assert.Empty(repository.Reports);
        Assert.Empty(incident.ReportIds);
    }

    [Fact]
    public async Task FileReport_ClosedIncident_ThrowsIncidentClosed()
    {
        var repository = new InMemoryDomainRepository();
        await RepositoryTestHelpers.RegisterHeroAsync(repository);
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        await Assert.ThrowsAsync<IncidentClosedException>(
            () => RepositoryTestHelpers.FileReportAsync(repository, incident.Id));
    }

    [Fact]
    public async Task ContextReportHelpers_TrackPendingAndAttachedReports()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);
        await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);

        var pending = await repository.ExecuteAsync(new InspectReportsCommand(incident.Id));
        Assert.Equal(new ReportCounts(2, 2), pending);

        await RepositoryTestHelpers.RegisterHeroAsync(repository);

        var attached = await repository.ExecuteAsync(new InspectReportsCommand(incident.Id));
        Assert.Equal(new ReportCounts(2, 0), attached);
    }

    private sealed record InspectReportsCommand(Guid IncidentId) : ICommand<ReportCounts>
    {
        public ReportCounts Execute(IDomainRepository repository, IDomainContext context) =>
            new(
                context.ReportsForIncident(IncidentId).Count(),
                context.PendingReportsForIncident(IncidentId).Count());
    }

    private sealed record ReportCounts(int Total, int Pending);
}
