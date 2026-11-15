using WWSHA.Domain;
using WWSHA.Persistence;
using WWSHA.Persistence.Commands;
using Xunit;

namespace WWSHA.Persistence.Tests;

public class CommandTests
{
    private static readonly DateTime Noon = new(2026, 11, 15, 12, 0, 0);

    private static (InMemoryDomainRepository store, IDomainContext context, IDomainRepository repo) NewStore()
    {
        var store = new InMemoryDomainRepository();
        return (store, store, store);
    }

    [Fact]
    public void Context_exposes_all_five_collections_as_read_only_views()
    {
        IDomainContext context = new InMemoryDomainRepository();

        Assert.Empty(context.Heroes);
        Assert.Empty(context.Incidents);
        Assert.Empty(context.Assignments);
        Assert.Empty(context.Reports);
        Assert.Empty(context.Assessments);
    }

    [Fact]
    public void Save_inserts_once_and_updates_in_place()
    {
        var (store, context, repo) = NewStore();
        var hero = new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);

        repo.Save(hero);
        repo.Save(hero);

        Assert.Single(store.Heroes);
    }

    [Fact]
    public void RegisterHero_stores_an_available_hero()
    {
        var (store, context, repo) = NewStore();

        var hero = new RegisterHeroCommand("The Monocle", "Sleep stare", Reputation: 3).Execute(context, repo);

        Assert.Same(hero, Assert.Single(store.Heroes));
        Assert.Equal(Availability.Available, hero.Availability);
        Assert.Equal(3, hero.Reputation);
    }

    [Fact]
    public void FileIncident_auto_assigns_the_first_available_hero()
    {
        var (store, context, repo) = NewStore();
        var hero = new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);

        var incident = new FileIncidentCommand("Redmond", Noon, "Bar brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);

        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Same(hero, Assert.Single(incident.Assignment!.Heroes));
        Assert.Equal(Availability.Unavailable, hero.Availability);
        Assert.Single(store.Assignments);
    }

    [Fact]
    public void FileIncident_stays_Opened_when_no_hero_is_available()
    {
        var (store, context, repo) = NewStore();
        var hero = new RegisterHeroCommand("Busy", "x").Execute(context, repo);
        new FileIncidentCommand("Seattle", Noon, "First", IncidentCategory.PettyCrime).Execute(context, repo);

        var second = new FileIncidentCommand("Seattle", Noon, "Second", IncidentCategory.WanderingKaiju)
            .Execute(context, repo);

        Assert.Equal(IncidentStatus.Opened, second.Status);
        Assert.Null(second.Assignment);
        Assert.Equal(2, store.Incidents.Count);
        Assert.Single(store.Assignments); // only the first incident got an assignment
        Assert.Equal(Availability.Unavailable, hero.Availability);
    }

    [Fact]
    public void AssignHero_adds_a_hero_to_an_opened_incident_once_one_is_free()
    {
        var (_, context, repo) = NewStore();
        var incident = new FileIncidentCommand("Seattle", Noon, "Kaiju", IncidentCategory.WanderingKaiju)
            .Execute(context, repo);
        var hero = new RegisterHeroCommand("Late Arrival", "Arrives late").Execute(context, repo);

        var assignment = new AssignHeroCommand(incident.Id, hero.Id).Execute(context, repo);

        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Same(hero, Assert.Single(assignment.Heroes));
    }

    [Fact]
    public void AssignHero_respects_the_three_hero_limit()
    {
        var (_, context, repo) = NewStore();
        var heroes = Enumerable.Range(1, 4)
            .Select(i => new RegisterHeroCommand($"Hero {i}", "x").Execute(context, repo))
            .ToList();
        var incident = new FileIncidentCommand("Seattle", Noon, "Brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);

        // Hero 1 was auto-assigned by FileIncident, so heroes 2 and 3 fill the remaining slots.
        new AssignHeroCommand(incident.Id, heroes[1].Id).Execute(context, repo);
        new AssignHeroCommand(incident.Id, heroes[2].Id).Execute(context, repo);

        Assert.Throws<AssignmentFullException>(() =>
            new AssignHeroCommand(incident.Id, heroes[3].Id).Execute(context, repo));
    }

    [Fact]
    public void AssignHero_rejects_a_hero_already_on_assignment()
    {
        var (_, context, repo) = NewStore();
        var hero = new RegisterHeroCommand("Busy", "x").Execute(context, repo);
        var first = new FileIncidentCommand("Seattle", Noon, "One", IncidentCategory.PettyCrime).Execute(context, repo);
        var second = new FileIncidentCommand("Seattle", Noon, "Two", IncidentCategory.PettyCrime).Execute(context, repo);

        Assert.Equal(IncidentStatus.InProgress, first.Status);
        Assert.Throws<HeroUnavailableException>(() =>
            new AssignHeroCommand(second.Id, hero.Id).Execute(context, repo));
        Assert.Equal(IncidentStatus.Opened, second.Status);
    }

    [Fact]
    public void Commands_throw_EntityNotFound_for_unknown_ids()
    {
        var (_, context, repo) = NewStore();
        var hero = new RegisterHeroCommand("Someone", "x").Execute(context, repo);

        Assert.Throws<EntityNotFoundException>(() =>
            new AssignHeroCommand(Guid.NewGuid(), hero.Id).Execute(context, repo));
        Assert.Throws<EntityNotFoundException>(() =>
            new CloseIncidentCommand(Guid.NewGuid()).Execute(context, repo));
    }

    [Fact]
    public void FileDamageReport_stores_the_report_in_the_assignment_history()
    {
        var (store, context, repo) = NewStore();
        new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);
        var incident = new FileIncidentCommand("Redmond", Noon, "Bar brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);

        var report = new FileDamageReportCommand(incident.Id, "Redmond", "Broken table", Noon.AddMinutes(40))
            .Execute(context, repo);

        Assert.Same(report, Assert.Single(store.Reports));
        Assert.Same(report, Assert.Single(incident.Assignment!.Reports));
    }

    [Fact]
    public void CloseIncident_assesses_heroes_and_frees_them()
    {
        var (store, context, repo) = NewStore();
        var hero = new RegisterHeroCommand("Captain Inconvenient", "Teleporting", Reputation: 1).Execute(context, repo);
        var incident = new FileIncidentCommand("Redmond", Noon, "Bar brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);
        new FileDamageReportCommand(incident.Id, "Redmond", "Broken table", Noon).Execute(context, repo);

        var result = new CloseIncidentCommand(incident.Id).Execute(context, repo);

        Assert.Equal(IncidentStatus.Closed, result.Incident.Status);
        Assert.Equal(Availability.Available, hero.Availability);
        Assert.Equal(5, hero.Reputation);                 // 1 + (5 - 1 report)
        Assert.Same(result.Assessments[0], Assert.Single(store.Assessments));
        Assert.Equal(4, result.Assessments[0].ReputationChange);
    }

    [Fact]
    public void Freed_hero_can_take_the_next_incident()
    {
        var (_, context, repo) = NewStore();
        var hero = new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);
        var first = new FileIncidentCommand("Redmond", Noon, "One", IncidentCategory.PettyCrime).Execute(context, repo);
        new CloseIncidentCommand(first.Id).Execute(context, repo);

        var second = new FileIncidentCommand("Seattle", Noon, "Two", IncidentCategory.AlienInvasion).Execute(context, repo);

        Assert.Same(hero, Assert.Single(second.Assignment!.Heroes));
    }
}
