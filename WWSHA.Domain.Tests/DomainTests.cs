using WWSHA.Domain;
using Xunit;

namespace WWSHA.Domain.Tests;

public class DomainTests
{
    private static readonly DateTime Noon = new(2026, 11, 15, 12, 0, 0);

    private static Incident BarFight() =>
        new("Redmond", Noon, "Rowdy sportsball fans brawling", IncidentCategory.DrunkenBarFight);

    private static Report FileReport(Incident incident, string city = "Redmond", DateTime? at = null) =>
        incident.FileReport(city, "Damage", ReportSeverity.Minor, cost: 100, injury: 0, filedAt: at ?? Noon);

    [Fact]
    public void Captain_Inconvenient_scenario_plays_out_as_described()
    {
        // Captain Inconvenient: reputation 1, no assessments, available.
        var captain = new Hero("Captain Inconvenient", "Teleports people 1-3 feet left", reputation: 1);
        var incident = BarFight();

        incident.AssignHero(captain, Noon);
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Equal(Availability.Unavailable, captain.Availability);

        // One damage report (broken table legs and glasses).
        FileReport(incident, at: new DateTime(2026, 11, 15, 12, 40, 0));

        incident.Close(Noon.AddHours(1));

        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal(Availability.Available, captain.Availability);
        Assert.Null(captain.CurrentIncidentId);
        Assert.Single(captain.Assessments);
        Assert.Equal(4, captain.Assessments[0].ReputationChange); // 5 - 1 report
        Assert.Equal(5, captain.Reputation);                       // 1 + 4
    }

    [Fact]
    public void Assigning_a_busy_hero_throws_and_leaves_incident_unchanged()
    {
        var hero = new Hero("Monocle", "Sleep-inducing stare");
        var first = BarFight();
        var second = new Incident("Seattle", Noon, "Kaiju sighting", IncidentCategory.WanderingKaiju);

        first.AssignHero(hero, Noon);

        Assert.Throws<HeroUnavailableException>(() => second.AssignHero(hero, Noon));
        Assert.Equal(IncidentStatus.Opened, second.Status);
        Assert.Null(second.Assignment);
    }

    [Fact]
    public void An_assignment_holds_at_most_three_heroes()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x"), Noon);
        incident.AssignHero(new Hero("B", "x"), Noon);
        incident.AssignHero(new Hero("C", "x"), Noon);

        Assert.Throws<AssignmentFullException>(() => incident.AssignHero(new Hero("D", "x"), Noon));
        Assert.Equal(3, incident.Assignment!.Heroes.Count);
    }

    [Fact]
    public void The_same_hero_cannot_be_added_to_an_assignment_twice()
    {
        var hero = new Hero("A", "x");
        var incident = BarFight();
        incident.AssignHero(hero, Noon);

        var ex = Assert.Throws<DuplicateAssignmentException>(() => incident.AssignHero(hero, Noon));
        Assert.Equal("assignment.duplicate", ex.Code);
        Assert.Single(incident.Assignment!.Heroes);
    }

    [Fact]
    public void Reports_are_accepted_while_opened_and_attached_when_a_hero_arrives()
    {
        var incident = BarFight();

        var early = FileReport(incident);
        Assert.Equal(IncidentStatus.Opened, incident.Status);
        Assert.Null(incident.Assignment);

        incident.AssignHero(new Hero("A", "x"), Noon);

        Assert.Same(early, Assert.Single(incident.Assignment!.Reports));
    }

    [Fact]
    public void Reports_are_rejected_after_the_incident_is_closed()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x"), Noon);
        incident.Close(Noon);

        var ex = Assert.Throws<IncidentClosedException>(() => FileReport(incident));
        Assert.Equal("incident.closed", ex.Code);
    }

    [Fact]
    public void Reports_must_come_from_the_incident_city()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x"), Noon);

        Assert.Throws<InvalidReportException>(() => FileReport(incident, city: "Seattle"));
    }

    [Fact]
    public void Reports_are_added_to_the_assignment_history_and_the_incident()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x"), Noon);

        var report = FileReport(incident, city: "redmond");

        Assert.Same(report, Assert.Single(incident.Reports));
        Assert.Same(report, Assert.Single(incident.Assignment!.Reports));
    }

    [Fact]
    public void Report_values_are_validated()
    {
        var incident = BarFight();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            incident.FileReport("Redmond", "x", ReportSeverity.Minor, cost: -1, injury: 0, filedAt: Noon));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            incident.FileReport("Redmond", "x", ReportSeverity.Minor, cost: 0, injury: -1, filedAt: Noon));
        Assert.Throws<InvalidReportException>(() =>
            incident.FileReport("Redmond", "x", (ReportSeverity)99, cost: 0, injury: 0, filedAt: Noon));
    }

    [Fact]
    public void Closing_an_opened_incident_is_rejected()
    {
        var incident = BarFight();

        var ex = Assert.Throws<InvalidIncidentStateException>(() => incident.Close(Noon));
        Assert.Equal("incident.state", ex.Code);
        Assert.Equal(IncidentStatus.Opened, incident.Status);
    }

    [Fact]
    public void Closing_twice_is_rejected_as_closed()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x"), Noon);
        incident.Close(Noon);

        Assert.Throws<IncidentClosedException>(() => incident.Close(Noon));
    }

    [Fact]
    public void Closing_stamps_the_assignment_and_rejects_further_changes()
    {
        var incident = BarFight();
        var hero = new Hero("A", "x");
        incident.AssignHero(hero, Noon);
        var closedAt = Noon.AddHours(2);

        incident.Close(closedAt);

        Assert.True(incident.Assignment!.IsClosed);
        Assert.Equal(closedAt, incident.Assignment.ClosedAt);
        Assert.Equal(Noon, incident.Assignment.CreatedAt);
    }

    [Fact]
    public void Assessment_must_match_the_hero_and_incident_it_is_recorded_against()
    {
        var hero = new Hero("A", "x");
        var incident = BarFight();
        incident.AssignHero(hero, Noon);

        var wrongHero = new Assessment(Guid.NewGuid(), incident.Id, 0, Noon);
        var wrongIncident = new Assessment(hero.Id, Guid.NewGuid(), 0, Noon);

        Assert.Throws<InvalidAssessmentException>(() => hero.RecordAssessment(wrongHero));
        Assert.Throws<InvalidAssessmentException>(() => hero.RecordAssessment(wrongIncident));
        Assert.Equal(Availability.Unavailable, hero.Availability);
    }

    [Fact]
    public void Heavy_damage_can_make_the_assessment_negative()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x", reputation: 0), Noon);
        for (int i = 0; i < 7; i++)
        {
            FileReport(incident);
        }

        incident.Close(Noon);

        var hero = incident.Assignment!.Heroes[0];
        Assert.Equal(-2, hero.Assessments[0].ReputationChange); // 5 - 7
        Assert.Equal(-2, hero.Reputation);
    }

    [Fact]
    public void Blank_names_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Hero(" ", "x"));
        Assert.Throws<ArgumentException>(() => new Hero("A", ""));
    }
}
