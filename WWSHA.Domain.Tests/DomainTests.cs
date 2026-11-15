using WWSHA.Domain;
using Xunit;

namespace WWSHA.Domain.Tests;

public class DomainTests
{
    private static Incident BarFight() =>
        new("Redmond", new DateTime(2026, 11, 15, 12, 0, 0), "Rowdy sportsball fans brawling", IncidentCategory.DrunkenBarFight);

    [Fact]
    public void Captain_Inconvenient_scenario_plays_out_as_described()
    {
        // Captain Inconvenient: reputation 1, no assessments, available.
        var captain = new Hero("Captain Inconvenient", "Teleports people 1-3 feet left", reputation: 1);
        var incident = BarFight();

        incident.AssignHero(captain);
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Equal(Availability.Unavailable, captain.Availability);

        // One damage report (broken table legs and glasses).
        incident.FileReport("Redmond", "Table legs busted, glasses broken", new DateTime(2026, 11, 15, 12, 40, 0));

        incident.Close();

        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal(Availability.Available, captain.Availability);
        Assert.Single(captain.Assessments);
        Assert.Equal(4, captain.Assessments[0].ReputationChange); // 5 - 1 report
        Assert.Equal(5, captain.Reputation);                       // 1 + 4
    }

    [Fact]
    public void Assigning_a_busy_hero_throws_and_leaves_incident_unchanged()
    {
        var hero = new Hero("Monocle", "Sleep-inducing stare");
        var first = BarFight();
        var second = new Incident("Seattle", DateTime.Now, "Kaiju sighting", IncidentCategory.WanderingKaiju);

        first.AssignHero(hero);

        Assert.Throws<HeroUnavailableException>(() => second.AssignHero(hero));
        Assert.Equal(IncidentStatus.Opened, second.Status);
        Assert.Null(second.Assignment);
    }

    [Fact]
    public void An_assignment_holds_at_most_three_heroes()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x"));
        incident.AssignHero(new Hero("B", "x"));
        incident.AssignHero(new Hero("C", "x"));

        Assert.Throws<AssignmentFullException>(() => incident.AssignHero(new Hero("D", "x")));
        Assert.Equal(3, incident.Assignment!.Heroes.Count);
    }

    [Fact]
    public void Reports_are_rejected_while_opened_or_closed()
    {
        var incident = BarFight();
        Assert.Throws<InvalidIncidentStateException>(() =>
            incident.FileReport("Redmond", "too early", DateTime.Now));

        incident.AssignHero(new Hero("A", "x"));
        incident.Close();
        Assert.Throws<InvalidIncidentStateException>(() =>
            incident.FileReport("Redmond", "too late", DateTime.Now));
    }

    [Fact]
    public void Reports_must_come_from_the_incident_city()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x"));

        Assert.Throws<InvalidReportException>(() =>
            incident.FileReport("Seattle", "wrong city", DateTime.Now));
    }

    [Fact]
    public void Reports_are_added_to_the_assignment_history()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x"));

        incident.FileReport("redmond", "case-insensitive city match", DateTime.Now);

        Assert.Single(incident.Assignment!.Reports);
    }

    [Fact]
    public void Closing_an_opened_incident_is_rejected()
    {
        var incident = BarFight();
        Assert.Throws<InvalidIncidentStateException>(() => incident.Close());
    }

    [Fact]
    public void Heavy_damage_can_make_the_assessment_negative()
    {
        var incident = BarFight();
        incident.AssignHero(new Hero("A", "x", reputation: 0));
        for (int i = 0; i < 7; i++)
            incident.FileReport("Redmond", $"report {i}", DateTime.Now);

        incident.Close();

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
