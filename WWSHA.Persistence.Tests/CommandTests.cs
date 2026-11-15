using WWSHA.Domain;
using WWSHA.Persistence;
using WWSHA.Persistence.Commands;
using Xunit;

namespace WWSHA.Persistence.Tests;

/// <summary>A clock the test controls, so timestamps are deterministic.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

public class CommandTests
{
    private static readonly DateTime Noon = new(2026, 11, 15, 12, 0, 0);

    private static readonly DateTimeOffset NoonUtc = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static (InMemoryDomainRepository store, IDomainContext context, IDomainRepository repo, FixedClock clock) NewStore()
    {
        var clock = new FixedClock(NoonUtc);
        var store = new InMemoryDomainRepository(clock);
        return (store, store, store, clock);
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
        var (store, context, repo, _) = NewStore();
        var hero = new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);

        repo.Save(hero);
        repo.Save(hero);

        Assert.Single(store.Heroes);
    }

    [Fact]
    public void Snapshots_do_not_change_when_the_store_changes()
    {
        var (store, context, repo, _) = NewStore();
        var snapshot = store.Heroes;

        new RegisterHeroCommand("Late", "x").Execute(context, repo);

        Assert.Empty(snapshot);
        Assert.Single(store.Heroes);
    }

    [Fact]
    public void RegisterHero_stores_an_available_hero()
    {
        var (store, context, repo, _) = NewStore();

        var hero = new RegisterHeroCommand("The Monocle", "Sleep stare", Reputation: 3).Execute(context, repo);

        Assert.Same(hero, Assert.Single(store.Heroes));
        Assert.Equal(Availability.Available, hero.Availability);
        Assert.Equal(3, hero.Reputation);
    }

    [Fact]
    public void FileIncident_auto_assigns_the_first_available_hero()
    {
        var (store, context, repo, _) = NewStore();
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
        var (store, context, repo, _) = NewStore();
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
    public void Registering_a_hero_dispatches_the_oldest_waiting_incident()
    {
        var (_, context, repo, _) = NewStore();
        var older = new FileIncidentCommand("Seattle", Noon.AddHours(-2), "Older", IncidentCategory.PettyCrime)
            .Execute(context, repo);
        var newer = new FileIncidentCommand("Seattle", Noon, "Newer", IncidentCategory.PettyCrime)
            .Execute(context, repo);

        var hero = new RegisterHeroCommand("Late Arrival", "Arrives late").Execute(context, repo);

        Assert.Equal(IncidentStatus.InProgress, older.Status);
        Assert.Same(hero, Assert.Single(older.Assignment!.Heroes));
        Assert.Equal(IncidentStatus.Opened, newer.Status);
    }

    [Fact]
    public void Closing_an_incident_dispatches_the_freed_hero_to_the_oldest_waiting_incident()
    {
        var (_, context, repo, _) = NewStore();
        var hero = new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);
        var working = new FileIncidentCommand("Redmond", Noon, "Working", IncidentCategory.PettyCrime)
            .Execute(context, repo);
        var waitingOld = new FileIncidentCommand("Seattle", Noon.AddHours(-1), "Old", IncidentCategory.PettyCrime)
            .Execute(context, repo);
        var waitingNew = new FileIncidentCommand("Seattle", Noon.AddHours(1), "New", IncidentCategory.PettyCrime)
            .Execute(context, repo);
        Assert.Equal(IncidentStatus.Opened, waitingOld.Status);
        Assert.Equal(IncidentStatus.Opened, waitingNew.Status);

        new CloseIncidentCommand(working.Id).Execute(context, repo);

        Assert.Equal(IncidentStatus.InProgress, waitingOld.Status);
        Assert.Same(hero, Assert.Single(waitingOld.Assignment!.Heroes));
        Assert.Equal(IncidentStatus.Opened, waitingNew.Status);
    }

    [Fact]
    public void Reports_filed_while_opened_are_attached_when_the_incident_is_dispatched()
    {
        var (store, context, repo, _) = NewStore();
        var incident = new FileIncidentCommand("Redmond", Noon, "Brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);
        var report = new FileDamageReportCommand(incident.Id, "Redmond", "Broken table",
            ReportSeverity.Minor, Cost: 200, Injury: 0).Execute(context, repo);

        new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);

        Assert.Same(report, Assert.Single(incident.Assignment!.Reports));
        Assert.Same(report, Assert.Single(store.Reports));
    }

    [Fact]
    public void Closing_an_opened_incident_through_the_command_is_rejected()
    {
        var (_, context, repo, _) = NewStore();
        var incident = new FileIncidentCommand("Redmond", Noon, "Brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);

        Assert.Throws<InvalidIncidentStateException>(() =>
            new CloseIncidentCommand(incident.Id).Execute(context, repo));
        Assert.Equal(IncidentStatus.Opened, incident.Status);
    }

    [Fact]
    public void AssignHero_respects_the_three_hero_limit()
    {
        var (_, context, repo, _) = NewStore();
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
        var (_, context, repo, _) = NewStore();
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
        var (_, context, repo, _) = NewStore();
        var hero = new RegisterHeroCommand("Someone", "x").Execute(context, repo);

        Assert.Throws<EntityNotFoundException>(() =>
            new AssignHeroCommand(Guid.NewGuid(), hero.Id).Execute(context, repo));
        Assert.Throws<EntityNotFoundException>(() =>
            new CloseIncidentCommand(Guid.NewGuid()).Execute(context, repo));
    }

    [Fact]
    public void FileDamageReport_stores_the_report_in_the_assignment_history()
    {
        var (store, context, repo, _) = NewStore();
        new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);
        var incident = new FileIncidentCommand("Redmond", Noon, "Bar brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);

        var report = new FileDamageReportCommand(incident.Id, "Redmond", "Broken table",
            ReportSeverity.Moderate, Cost: 500, Injury: 1, FiledAt: Noon.AddMinutes(40)).Execute(context, repo);

        Assert.Same(report, Assert.Single(store.Reports));
        Assert.Same(report, Assert.Single(incident.Assignment!.Reports));
        Assert.Equal(ReportSeverity.Moderate, report.Severity);
        Assert.Equal(500, report.Cost);
        Assert.Equal(1, report.Injury);
    }

    [Fact]
    public void FileDamageReport_uses_the_context_clock_when_no_time_is_given()
    {
        var (_, context, repo, clock) = NewStore();
        new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);
        var incident = new FileIncidentCommand("Redmond", Noon, "Bar brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);
        clock.Now = NoonUtc.AddMinutes(40);

        var report = new FileDamageReportCommand(incident.Id, "Redmond", "Broken table",
            ReportSeverity.Minor, Cost: 0, Injury: 0).Execute(context, repo);

        Assert.Equal(clock.Now.LocalDateTime, report.FiledAt);
    }

    [Fact]
    public void CloseIncident_assesses_heroes_and_frees_them()
    {
        var (store, context, repo, clock) = NewStore();
        var hero = new RegisterHeroCommand("Captain Inconvenient", "Teleporting", Reputation: 1).Execute(context, repo);
        var incident = new FileIncidentCommand("Redmond", Noon, "Bar brawl", IncidentCategory.DrunkenBarFight)
            .Execute(context, repo);
        new FileDamageReportCommand(incident.Id, "Redmond", "Broken table",
            ReportSeverity.Minor, Cost: 0, Injury: 0).Execute(context, repo);
        clock.Now = NoonUtc.AddHours(1);

        var result = new CloseIncidentCommand(incident.Id).Execute(context, repo);

        Assert.Equal(IncidentStatus.Closed, result.Incident.Status);
        Assert.Equal(Availability.Available, hero.Availability);
        Assert.Equal(5, hero.Reputation);                 // 1 + (5 - 1 report)
        Assert.Same(result.Assessments[0], Assert.Single(store.Assessments));
        Assert.Equal(4, result.Assessments[0].ReputationChange);
        Assert.Equal(clock.Now.LocalDateTime, incident.Assignment!.ClosedAt);
    }

    [Fact]
    public void Freed_hero_can_take_the_next_incident()
    {
        var (_, context, repo, _) = NewStore();
        var hero = new RegisterHeroCommand("Captain Inconvenient", "Teleporting").Execute(context, repo);
        var first = new FileIncidentCommand("Redmond", Noon, "One", IncidentCategory.PettyCrime).Execute(context, repo);
        new CloseIncidentCommand(first.Id).Execute(context, repo);

        var second = new FileIncidentCommand("Seattle", Noon, "Two", IncidentCategory.AlienInvasion).Execute(context, repo);

        Assert.Same(hero, Assert.Single(second.Assignment!.Heroes));
    }
}
