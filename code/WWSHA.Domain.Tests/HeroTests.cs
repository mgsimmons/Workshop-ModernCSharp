using WWSHA.Domain;

namespace WWSHA.Domain.Tests;

public sealed class HeroTests
{
    [Fact]
    public void Register_ValidHero_CreatesAvailableHeroWithZeroReputation()
    {
        var hero = Hero.Register("The Sloth", "Extreme procrastination");

        Assert.NotEqual(Guid.Empty, hero.Id);
        Assert.Equal("The Sloth", hero.Name);
        Assert.Equal("Extreme procrastination", hero.Superpower);
        Assert.Equal(HeroAvailability.Available, hero.Availability);
        Assert.Equal(0, hero.Reputation);
        Assert.Null(hero.CurrentIncidentId);
        Assert.Empty(hero.Assessments);
    }

    [Fact]
    public void Register_TrimsNameAndSuperpower()
    {
        var hero = Hero.Register("  The Sloth  ", "  Extreme procrastination  ");

        Assert.Equal("The Sloth", hero.Name);
        Assert.Equal("Extreme procrastination", hero.Superpower);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_BlankName_Throws(string? name)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Hero.Register(name!, "Superpower"));

        Assert.Equal("validation.failed", exception.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_BlankSuperpower_Throws(string? superpower)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Hero.Register("The Sloth", superpower!));

        Assert.Equal("validation.failed", exception.Code);
    }

    [Fact]
    public void AssignTo_AvailableHero_MarksHeroAssigned()
    {
        var hero = Hero.Register("The Sloth", "Extreme procrastination");
        var incidentId = Guid.NewGuid();

        hero.AssignTo(incidentId);

        Assert.Equal(HeroAvailability.Assigned, hero.Availability);
        Assert.Equal(incidentId, hero.CurrentIncidentId);
    }

    [Fact]
    public void AssignTo_WhenAlreadyAssigned_ThrowsHeroUnavailable()
    {
        var hero = Hero.Register("The Sloth", "Extreme procrastination");
        hero.AssignTo(Guid.NewGuid());

        var exception = Assert.Throws<HeroUnavailableException>(() => hero.AssignTo(Guid.NewGuid()));

        Assert.Equal("hero.unavailable", exception.Code);
    }

    [Fact]
    public void AssignTo_EmptyIncidentId_Throws()
    {
        var hero = Hero.Register("The Sloth", "Extreme procrastination");

        Assert.Throws<DomainValidationException>(() => hero.AssignTo(Guid.Empty));
    }

    [Fact]
    public void CompleteAssignment_ValidAssessment_AppliesReputationAndFreesHero()
    {
        var hero = Hero.Register("Captain Maybe", "Hesitation");
        var incidentId = Guid.NewGuid();
        hero.AssignTo(incidentId);
        var assessment = Assessment.Create(hero.Id, incidentId, 3, DateTime.UtcNow);

        hero.CompleteAssignment(assessment);

        Assert.Equal(2, hero.Reputation);
        Assert.Equal(HeroAvailability.Available, hero.Availability);
        Assert.Null(hero.CurrentIncidentId);
        Assert.Same(assessment, Assert.Single(hero.Assessments));
    }

    [Fact]
    public void CompleteAssignment_PoorAssessment_AllowsNegativeReputation()
    {
        var hero = Hero.Register("Captain Maybe", "Hesitation");
        var incidentId = Guid.NewGuid();
        hero.AssignTo(incidentId);

        hero.CompleteAssignment(Assessment.Create(hero.Id, incidentId, 7, DateTime.UtcNow));

        Assert.Equal(-2, hero.Reputation);
    }

    [Fact]
    public void CompleteAssignment_AccumulatesReputationAcrossAssignments()
    {
        var hero = Hero.Register("Captain Maybe", "Hesitation");

        var firstIncident = Guid.NewGuid();
        hero.AssignTo(firstIncident);
        hero.CompleteAssignment(Assessment.Create(hero.Id, firstIncident, 0, DateTime.UtcNow));

        var secondIncident = Guid.NewGuid();
        hero.AssignTo(secondIncident);
        hero.CompleteAssignment(Assessment.Create(hero.Id, secondIncident, 7, DateTime.UtcNow));

        Assert.Equal(3, hero.Reputation);
        Assert.Equal(2, hero.Assessments.Count);
    }

    [Fact]
    public void CompleteAssignment_NullAssessment_Throws()
    {
        var hero = Hero.Register("Captain Maybe", "Hesitation");

        Assert.Throws<DomainValidationException>(() => hero.CompleteAssignment(null!));
    }

    [Fact]
    public void CompleteAssignment_AssessmentForAnotherHero_Throws()
    {
        var hero = Hero.Register("Captain Maybe", "Hesitation");
        var otherHero = Hero.Register("The Sloth", "Extreme procrastination");
        var incidentId = Guid.NewGuid();
        hero.AssignTo(incidentId);
        var assessment = Assessment.Create(otherHero.Id, incidentId, 0, DateTime.UtcNow);

        Assert.Throws<DomainValidationException>(() => hero.CompleteAssignment(assessment));
    }

    [Fact]
    public void CompleteAssignment_WhenHeroNotAssigned_ThrowsInvalidIncidentState()
    {
        var hero = Hero.Register("Captain Maybe", "Hesitation");
        var assessment = Assessment.Create(hero.Id, Guid.NewGuid(), 0, DateTime.UtcNow);

        var exception = Assert.Throws<InvalidIncidentStateException>(
            () => hero.CompleteAssignment(assessment));

        Assert.Equal("incident.invalid_state", exception.Code);
    }

    [Fact]
    public void CompleteAssignment_WhenIncidentDoesNotMatch_ThrowsInvalidIncidentState()
    {
        var hero = Hero.Register("Captain Maybe", "Hesitation");
        hero.AssignTo(Guid.NewGuid());
        var assessment = Assessment.Create(hero.Id, Guid.NewGuid(), 0, DateTime.UtcNow);

        Assert.Throws<InvalidIncidentStateException>(() => hero.CompleteAssignment(assessment));
    }

    [Fact]
    public void Assessments_ReturnedCollectionIsReadOnly()
    {
        var hero = Hero.Register("Captain Maybe", "Hesitation");
        var assessments = Assert.IsAssignableFrom<IList<Assessment>>(hero.Assessments);
        var assessment = Assessment.Create(hero.Id, Guid.NewGuid(), 0, DateTime.UtcNow);

        Assert.True(assessments.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => assessments.Add(assessment));
    }
}
