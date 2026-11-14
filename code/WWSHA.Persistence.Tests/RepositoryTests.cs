using WWSHA.Domain;
using WWSHA.Persistence.Commands;
using WWSHA.Persistence.InMemory;

namespace WWSHA.Persistence.Tests;

public sealed class RepositoryTests
{
    [Fact]
    public async Task RegisterHero_PersistsAndExposesItThroughQueries()
    {
        var repository = new InMemoryDomainRepository();

        var hero = await repository.ExecuteAsync(
            new RegisterHeroCommand("The Sloth", "Extreme procrastination"));

        Assert.Same(hero, Assert.Single(repository.Heroes));
    }

    [Fact]
    public async Task ExecuteAsync_NonGenericCommand_RunsCommand()
    {
        var repository = new InMemoryDomainRepository();
        var hero = Hero.Register("The Sloth", "Extreme procrastination");

        await repository.ExecuteAsync(new AddHeroCommand(hero));

        Assert.Same(hero, Assert.Single(repository.Heroes));
    }

    [Fact]
    public async Task ExecuteAsync_CanceledToken_DoesNotRunCommand()
    {
        var repository = new InMemoryDomainRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.ExecuteAsync(
                new RegisterHeroCommand("The Sloth", "Extreme procrastination"),
                cancellation.Token));

        Assert.Empty(repository.Heroes);
    }

    [Fact]
    public async Task ExecuteAsync_NullCommand_ThrowsArgumentNullException()
    {
        var repository = new InMemoryDomainRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.ExecuteAsync((ICommand)null!));
    }

    [Fact]
    public async Task Queries_ReturnSnapshotsThatDoNotObserveLaterWrites()
    {
        var repository = new InMemoryDomainRepository();
        var heroes = repository.Heroes;

        await RepositoryTestHelpers.RegisterHeroAsync(repository);

        Assert.Empty(heroes);
        Assert.Single(repository.Heroes);
    }

    [Fact]
    public async Task Assessments_AreDerivedFromHeroes()
    {
        var repository = new InMemoryDomainRepository();
        var hero = await RepositoryTestHelpers.RegisterHeroAsync(repository);
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        await repository.ExecuteAsync(new CloseIncidentCommand(incident.Id));

        var assessment = Assert.Single(repository.Assessments);
        Assert.Equal(hero.Id, assessment.HeroId);
        Assert.Equal(incident.Id, assessment.IncidentId);
    }

    [Fact]
    public async Task MissingIncident_ThrowsEntityNotFoundException()
    {
        var repository = new InMemoryDomainRepository();

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => repository.ExecuteAsync(
                new AssignHeroCommand(Guid.NewGuid(), Guid.NewGuid())));

        Assert.Equal("entity.not_found", exception.Code);
    }

    [Fact]
    public async Task MissingHero_ThrowsEntityNotFoundException()
    {
        var repository = new InMemoryDomainRepository();
        var incident = await RepositoryTestHelpers.FileIncidentAsync(repository);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => repository.ExecuteAsync(new AssignHeroCommand(incident.Id, Guid.NewGuid())));

        Assert.Equal("entity.not_found", exception.Code);
    }

    private sealed record AddHeroCommand(Hero Hero) : ICommand
    {
        public void Execute(IDomainRepository repository, IDomainContext context) => repository.AddHero(Hero);
    }
}
