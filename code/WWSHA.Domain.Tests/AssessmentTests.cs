using WWSHA.Domain;

namespace WWSHA.Domain.Tests;

public sealed class AssessmentTests
{
    [Theory]
    [InlineData(0, 5)]
    [InlineData(3, 2)]
    [InlineData(5, 0)]
    [InlineData(7, -2)]
    public void Create_ComputesReputationDeltaFromDamageReportCount(
        int damageReportCount,
        int expectedDelta)
    {
        var assessment = Assessment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            damageReportCount,
            DateTime.UtcNow);

        Assert.Equal(expectedDelta, assessment.ReputationDelta);
    }

    [Fact]
    public void Create_SetsExpectedFields()
    {
        var heroId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 11, 11, 12, 30, 0, DateTimeKind.Utc);

        var assessment = Assessment.Create(heroId, incidentId, 4, createdAt);

        Assert.NotEqual(Guid.Empty, assessment.Id);
        Assert.Equal(heroId, assessment.HeroId);
        Assert.Equal(incidentId, assessment.IncidentId);
        Assert.Equal(4, assessment.DamageReportCount);
        Assert.Equal(1, assessment.ReputationDelta);
        Assert.Equal(createdAt, assessment.CreatedAt);
    }

    [Fact]
    public void Create_EmptyHeroId_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Assessment.Create(Guid.Empty, Guid.NewGuid(), 0, DateTime.UtcNow));

        Assert.Equal("validation.failed", exception.Code);
    }

    [Fact]
    public void Create_EmptyIncidentId_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => Assessment.Create(Guid.NewGuid(), Guid.Empty, 0, DateTime.UtcNow));
    }

    [Fact]
    public void Create_NegativeDamageReportCount_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => Assessment.Create(Guid.NewGuid(), Guid.NewGuid(), -1, DateTime.UtcNow));
    }
}
