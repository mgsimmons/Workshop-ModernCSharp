using WWSHA.Domain;
using Xunit;

namespace WWSHA.Domain.Tests;

/// <summary>
/// Rule guards that are reachable through the public domain API, plus the defensive checks inside
/// <see cref="Assignment"/> that the public flow never reaches, because a closed assignment's
/// incident is already closed and rejected first.
/// </summary>
public class GuardTests
{
    private static readonly DateTime Noon = new(2026, 11, 15, 12, 0, 0);

    private static Incident Brawl() =>
        new("Redmond", Noon, "Brawl", IncidentCategory.DrunkenBarFight);

    [Fact]
    public void Assigning_a_hero_to_a_closed_incident_is_rejected()
    {
        var incident = Brawl();
        incident.AssignHero(new Hero("A", "x"), Noon);
        incident.Close(Noon);

        var ex = Assert.Throws<IncidentClosedException>(() => incident.AssignHero(new Hero("B", "x"), Noon));
        Assert.Equal("incident.closed", ex.Code);
    }

    [Fact]
    public void An_available_hero_cannot_be_assessed()
    {
        var hero = new Hero("A", "x");
        var incident = Brawl();
        incident.AssignHero(hero, Noon);
        incident.Close(Noon);

        var stale = new Assessment(hero.Id, incident.Id, 0, Noon);
        var ex = Assert.Throws<InvalidAssessmentException>(() => hero.RecordAssessment(stale));
        Assert.Equal("assessment.invalid", ex.Code);
    }

    [Fact]
    public void An_assessment_for_another_incident_is_rejected_while_working()
    {
        var hero = new Hero("A", "x");
        var incident = Brawl();
        incident.AssignHero(hero, Noon);

        var other = new Assessment(hero.Id, Guid.NewGuid(), 0, Noon);
        Assert.Throws<InvalidAssessmentException>(() => hero.RecordAssessment(other));
        Assert.Equal(Availability.Unavailable, hero.Availability);
    }

    [Fact]
    public void Assessing_a_hero_with_a_null_assessment_throws()
    {
        var hero = new Hero("A", "x");
        Assert.Throws<ArgumentNullException>(() => hero.RecordAssessment(null!));
    }

    [Fact]
    public void Assignment_rejects_a_hero_once_it_is_closed()
    {
        var incident = Brawl();
        var assignment = new Assignment(incident, Noon);
        assignment.Close(Noon);

        var ex = Assert.Throws<InvalidIncidentStateException>(() => assignment.AddHero(new Hero("A", "x")));
        Assert.Equal("incident.state", ex.Code);
    }

    [Fact]
    public void Assignment_rejects_a_report_once_it_is_closed()
    {
        var incident = Brawl();
        var assignment = new Assignment(incident, Noon);
        var report = incident.FileReport("Redmond", "x", ReportSeverity.Minor, 0, 0, Noon);
        assignment.Close(Noon);

        Assert.Throws<InvalidIncidentStateException>(() => assignment.AddReport(report));
    }

    [Fact]
    public void Assignment_rejects_the_same_report_twice()
    {
        var incident = Brawl();
        var assignment = new Assignment(incident, Noon);
        var report = incident.FileReport("Redmond", "x", ReportSeverity.Minor, 0, 0, Noon);
        assignment.AddReport(report);

        var ex = Assert.Throws<InvalidReportException>(() => assignment.AddReport(report));
        Assert.Equal("report.invalid", ex.Code);
    }

    [Fact]
    public void Assignment_cannot_be_closed_twice()
    {
        var assignment = new Assignment(Brawl(), Noon);
        assignment.Close(Noon);

        Assert.Throws<InvalidIncidentStateException>(() => assignment.Close(Noon.AddHours(1)));
    }

    [Fact]
    public void Assignment_is_full_at_three_heroes_even_when_called_directly()
    {
        var assignment = new Assignment(Brawl(), Noon);
        for (int i = 0; i < Assignment.MaxHeroes; i++)
            assignment.AddHero(new Hero($"H{i}", "x"));

        var ex = Assert.Throws<AssignmentFullException>(() => assignment.AddHero(new Hero("Extra", "x")));
        Assert.Equal("assignment.full", ex.Code);
    }

    [Fact]
    public void Every_domain_exception_carries_a_code()
    {
        var codes = new[]
        {
            new HeroUnavailableException("A").Code,
            new AssignmentFullException(3).Code,
            new DuplicateAssignmentException("A").Code,
            new IncidentClosedException(Guid.NewGuid()).Code,
            new InvalidIncidentStateException("x").Code,
            new InvalidReportException("x").Code,
            new InvalidIncidentException("x").Code,
            new InvalidAssessmentException("x").Code,
            new DomainException("x").Code,
        };

        Assert.All(codes, code => Assert.False(string.IsNullOrWhiteSpace(code)));
        Assert.Equal(codes.Length, codes.Distinct().Count()); // each exception type has its own code
    }
}
