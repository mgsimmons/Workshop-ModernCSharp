using WWSHA.Domain;
using WWSHA.Persistence.Commands;
using WWSHA.Persistence.InMemory;

namespace WWSHA.Persistence.Tests;

public sealed class DispatchTests
{
    [Fact]
    public async Task FileIncident_WithAvailableHero_AssignsHeroAndStartsWork()
    {
        var repository = new InMemoryDomainRepository();
        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);

        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        var assignment = Assert.Single(repository.Assignments);
        Assert.Equal(incident.Id, assignment.IncidentId);
        Assert.Equal(hero.Id, Assert.Single(assignment.HeroIds));

        var storedHero = Assert.Single(repository.Heroes);
        Assert.Equal(HeroAvailability.Assigned, storedHero.Availability);
        Assert.Equal(incident.Id, storedHero.CurrentIncidentId);
    }

    [Fact]
    public async Task FileIncident_WithNoAvailableHero_LeavesIncidentOpened()
    {
        var repository = new InMemoryDomainRepository();
        await RepositoryTestHelpers.RegisterHeroAsync(repository);

        var firstIncident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        var secondIncident = await RepositoryTestHelpers.FileIncidentAsync(
            repository,
            DateTime.Now.AddMinutes(1));

        Assert.Equal(IncidentStatus.InProgress, firstIncident.Status);
        Assert.Equal(IncidentStatus.Opened, secondIncident.Status);
        Assert.Single(repository.Assignments);
    }

    [Fact]
    public async Task RegisterHero_WithWaitingOpenedIncident_AutoAssignsToOldest()
    {
        var repository = new InMemoryDomainRepository();
        var older = await RepositoryTestHelpers.FileIncidentAsync(
            repository,
            new DateTime(2026, 11, 11, 8, 0, 0));
        var newer = await RepositoryTestHelpers.FileIncidentAsync(
            repository,
            new DateTime(2026, 11, 11, 9, 0, 0));

        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);

        Assert.Equal(IncidentStatus.InProgress, older.Status);
        Assert.Equal(IncidentStatus.Opened, newer.Status);
        var assignment = Assert.Single(repository.Assignments);
        Assert.Equal(older.Id, assignment.IncidentId);
        Assert.Equal(hero.Id, Assert.Single(assignment.HeroIds));
    }

    [Fact]
    public async Task RegisterHeroes_DispatchesOneHeroPerOpenedIncidentInOccurrenceOrder()
    {
        var repository = new InMemoryDomainRepository();
        var firstIncident = await RepositoryTestHelpers.FileIncidentAsync(
            repository,
            new DateTime(2026, 11, 11, 8, 0, 0));
        var secondIncident = await RepositoryTestHelpers.FileIncidentAsync(
            repository,
            new DateTime(2026, 11, 11, 9, 0, 0));

        var firstHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "First");
        var secondHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "Second");

        Assert.Equal(IncidentStatus.InProgress, firstIncident.Status);
        Assert.Equal(IncidentStatus.InProgress, secondIncident.Status);

        var assignments = repository.Assignments.ToArray();
        Assert.Equal(2, assignments.Length);
        Assert.Equal(
            firstHero.Id,
            assignments.Single(assignment => assignment.IncidentId == firstIncident.Id).HeroIds.Single());
        Assert.Equal(
            secondHero.Id,
            assignments.Single(assignment => assignment.IncidentId == secondIncident.Id).HeroIds.Single());
    }

    [Fact]
    public async Task AssignHero_ExplicitHero_AddsToExistingAssignment()
    {
        var repository = new InMemoryDomainRepository();
        var firstHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "First");
        var secondHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "Second");
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        var assignment = await repository.ExecuteAsync(
            new AssignHeroCommand(incident.Id, secondHero.Id));

        Assert.Equal(2, assignment.HeroIds.Count);
        Assert.Contains(firstHero.Id, assignment.HeroIds);
        Assert.Contains(secondHero.Id, assignment.HeroIds);
    }

    [Fact]
    public async Task AssignHero_WithoutHeroId_PicksFirstAvailableHero()
    {
        var repository = new InMemoryDomainRepository();
        await RepositoryTestHelpers.RegisterHeroAsync(repository, "First");
        var secondHero = await RepositoryTestHelpers.RegisterHeroAsync(repository, "Second");
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        var assignment = await repository.ExecuteAsync(new AssignHeroCommand(incident.Id));

        Assert.Equal(secondHero.Id, assignment.HeroIds[^1]);
    }

    [Fact]
    public async Task AssignHero_SecondAndThirdHeroes_AreAcceptedAndFourthIsRejected()
    {
        var repository = new InMemoryDomainRepository();
        var heroes = new List<Hero>();
        for (var index = 0; index < 4; index++)
        {
            heroes.Add(await RepositoryTestHelpers.RegisterHeroAsync(repository, $"Hero {index}"));
        }

        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await repository.ExecuteAsync(new AssignHeroCommand(incident.Id, heroes[1].Id));
        await repository.ExecuteAsync(new AssignHeroCommand(incident.Id, heroes[2].Id));

        var exception = await Assert.ThrowsAsync<AssignmentFullException>(
            () => repository.ExecuteAsync(new AssignHeroCommand(incident.Id, heroes[3].Id)));

        Assert.Equal("assignment.full", exception.Code);
        Assert.Equal(3, Assert.Single(repository.Assignments).HeroIds.Count);
    }

    [Fact]
    public async Task AssignHero_BusyHero_ThrowsHeroUnavailable()
    {
        var repository = new InMemoryDomainRepository();
        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        var exception = await Assert.ThrowsAsync<HeroUnavailableException>(
            () => repository.ExecuteAsync(new AssignHeroCommand(incident.Id, hero.Id)));

        Assert.Equal("hero.unavailable", exception.Code);
    }

    [Fact]
    public async Task AssignHero_NoAvailableHero_ThrowsHeroUnavailable()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        await Assert.ThrowsAsync<HeroUnavailableException>(
            () => repository.ExecuteAsync(new AssignHeroCommand(incident.Id)));
    }

    [Fact]
    public async Task AssignHero_ClosedIncident_ThrowsIncidentClosed()
    {
        var repository = new InMemoryDomainRepository();
        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);
        await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        var exception = await Assert.ThrowsAsync<IncidentClosedException>(
            () => repository.ExecuteAsync(new AssignHeroCommand(incident.Id, hero.Id)));

        Assert.Equal("incident.closed", exception.Code);
    }
}
