using WWSHA.Domain;
using WWSHA.Persistence.Commands;
using WWSHA.Persistence.InMemory;

namespace WWSHA.Persistence.Tests;

public sealed class CloseIncidentTests
{
    [Fact]
    public async Task CloseIncident_WithoutAssignment_ClosesAndReturnsNoAssessments()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        var result = await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        Assert.True(result.Incident.IsClosed);
        Assert.Empty(result.Assessments);
        Assert.Empty(repository.Assessments);
    }

    [Fact]
    public async Task CloseIncident_CreatesAssessmentAndUpdatesReputation()
    {
        var repository = new InMemoryDomainRepository();
        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);
        await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);

        var result = await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        var assessment = Assert.Single(result.Assessments);
        Assert.Equal(hero.Id, assessment.HeroId);
        Assert.Equal(incident.Id, assessment.IncidentId);
        Assert.Equal(2, assessment.DamageReportCount);
        Assert.Equal(3, assessment.ReputationDelta);
        Assert.Equal(3, hero.Reputation);
        Assert.Equal(HeroAvailability.Available, hero.Availability);
        Assert.Null(hero.CurrentIncidentId);
        Assert.True(incident.IsClosed);

        var assignment = Assert.Single(repository.Assignments);
        Assert.True(assignment.IsClosed);
        Assert.NotNull(assignment.ClosedAt);
        Assert.Same(assessment, Assert.Single(repository.Assessments));
    }

    [Fact]
    public async Task CloseIncident_CountsReportsFiledBeforeAssignment()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);
        await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);
        await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);

        await RepositoryTestHelpers.RegisterHeroAsync(repository);

        var assignment = Assert.Single(repository.Assignments);
        Assert.Equal(3, assignment.DamageReportIds.Count);

        var result = await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        var assessment = Assert.Single(result.Assessments);
        Assert.Equal(3, assessment.DamageReportCount);
        Assert.Equal(2, assessment.ReputationDelta);
    }

    [Fact]
    public async Task CloseIncident_ReputationCanGoNegative()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        for (var index = 0; index < 7; index++)
        {
            await RepositoryTestHelpers.FileReportAsync(repository, incident.Id);
        }

        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);

        var result = await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        var assessment = Assert.Single(result.Assessments);
        Assert.Equal(7, assessment.DamageReportCount);
        Assert.Equal(-2, assessment.ReputationDelta);
        Assert.Equal(-2, hero.Reputation);
    }

    [Fact]
    public async Task CloseIncident_CreatesOneAssessmentPerHero()
    {
        var repository = new InMemoryDomainRepository();
        var firstHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "First");
        var secondHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "Second");
        var thirdHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "Third");
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await repository.ExecuteAsync(new AssignHeroCommand(incident.Id, secondHero.Id));
        await repository.ExecuteAsync(new AssignHeroCommand(incident.Id, thirdHero.Id));

        var result = await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        Assert.Equal(3, result.Assessments.Count);
        Assert.Equal(3, repository.Assessments.Count());
        Assert.All(
            new[] { firstHero, secondHero, thirdHero },
            hero => Assert.Equal(HeroAvailability.Available, hero.Availability));
    }

    [Fact]
    public async Task CloseIncident_FreesHeroesAndDispatchesToOldestOpenedIncident()
    {
        var repository = new InMemoryDomainRepository();
        var firstHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "First");
        var secondHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "Second");
        var firstIncident = await RepositoryTestHelpers.FileIncidentAsync(
            repository,
            new DateTime(2026, 11, 11, 8, 0, 0));
        var secondIncident = await RepositoryTestHelpers.FileIncidentAsync(
            repository,
            new DateTime(2026, 11, 11, 9, 0, 0));
        var waitingIncident = await RepositoryTestHelpers.FileIncidentAsync(
            repository,
            new DateTime(2026, 11, 11, 10, 0, 0));

        Assert.Equal(IncidentStatus.Opened, waitingIncident.Status);

        await repository.ExecuteAsync(new CloseIncidentCommand(firstIncident.Id));

        Assert.Equal(HeroAvailability.Assigned, firstHero.Availability);
        Assert.Equal(waitingIncident.Id, firstHero.CurrentIncidentId);
        Assert.Equal(IncidentStatus.InProgress, waitingIncident.Status);
        Assert.Equal(HeroAvailability.Assigned, secondHero.Availability);
        Assert.Equal(secondIncident.Id, secondHero.CurrentIncidentId);
    }

    [Fact]
    public async Task CloseIncident_Twice_ThrowsIncidentClosed()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        var exception = await Assert.ThrowsAsync<IncidentClosedException>(
            () => repository.ExecuteAsync(new CloseIncidentCommand(incident.Id)));

        Assert.Equal("incident.closed", exception.Code);
    }

    [Fact]
    public async Task CloseIncident_MissingIncident_ThrowsEntityNotFound()
    {
        var repository = new InMemoryDomainRepository();

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => repository.ExecuteAsync(new CloseIncidentCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task ClosedIncident_RejectsNewAssignmentsAndReports()
    {
        var repository = new InMemoryDomainRepository();
        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        await Assert.ThrowsAsync<IncidentClosedException>(
            () => repository.ExecuteAsync(new AssignHeroCommand(incident.Id, hero.Id)));
        await Assert.ThrowsAsync<IncidentClosedException>(
            () => RepositoryTestHelpers.FileReportAsync(repository, incident.Id));
    }
}
