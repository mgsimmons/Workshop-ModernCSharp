using WWSHA.Domain;
using Xunit;

namespace WWSHA.Domain.Tests;

public class LimitAndCloseTests
{
    private static readonly DateTime Noon = new(2026, 11, 15, 12, 0, 0);

    [Fact]
    public void Names_longer_than_the_limit_are_rejected()
    {
        var tooLong = new string('x', DomainLimits.NameMaxLength + 1);

        Assert.Throws<ArgumentException>(() => new Hero(tooLong, "x"));
        Assert.Throws<ArgumentException>(() => new Hero("A", new string('x', DomainLimits.SuperpowerMaxLength + 1)));
    }

    [Fact]
    public void Names_at_the_limit_are_accepted()
    {
        var hero = new Hero(new string('x', DomainLimits.NameMaxLength), "x");

        Assert.Equal(DomainLimits.NameMaxLength, hero.Name.Length);
    }

    [Fact]
    public void Starting_reputation_must_be_within_the_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Hero("A", "x", DomainLimits.StartingReputationMax + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Hero("A", "x", DomainLimits.StartingReputationMin - 1));
    }

    [Fact]
    public void Report_cost_and_injury_have_upper_bounds()
    {
        var incident = new Incident("Redmond", Noon, "Brawl", IncidentCategory.DrunkenBarFight);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            incident.FileReport("Redmond", "x", ReportSeverity.Minor, DomainLimits.CostMax + 1, 0, Noon));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            incident.FileReport("Redmond", "x", ReportSeverity.Minor, 0, DomainLimits.InjuryMax + 1, Noon));
    }

    [Fact]
    public void Descriptions_longer_than_the_limit_are_rejected()
    {
        var incident = new Incident("Redmond", Noon, "Brawl", IncidentCategory.DrunkenBarFight);
        var tooLong = new string('x', DomainLimits.DescriptionMaxLength + 1);

        Assert.Throws<ArgumentException>(() =>
            incident.FileReport("Redmond", tooLong, ReportSeverity.Minor, 0, 0, Noon));
        Assert.Throws<ArgumentException>(() =>
            new Incident("Redmond", Noon, tooLong, IncidentCategory.PettyCrime));
    }

    [Fact]
    public void A_rejected_close_changes_nothing()
    {
        var hero = new Hero("Captain Inconvenient", "x", reputation: 1);
        var incident = new Incident("Redmond", Noon, "Brawl", IncidentCategory.DrunkenBarFight);
        incident.AssignHero(hero, Noon);
        incident.Close(Noon);

        // The incident is already closed, so this close is rejected before any hero is touched.
        Assert.Throws<IncidentClosedException>(() => incident.Close(Noon));

        Assert.Single(hero.Assessments);
        Assert.Equal(6, hero.Reputation); // 1 starting + 5 (no reports on this incident)
        Assert.Equal(Availability.Available, hero.Availability);
    }
}
