using WWSHA.Domain;

namespace WWSHA.Domain.Tests;

public sealed class AssignmentTests
{
    [Fact]
    public void Create_ValidIncident_CreatesOpenAssignment()
    {
        var incidentId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

        var assignment = Assignment.Create(incidentId, createdAt);

        Assert.NotEqual(Guid.Empty, assignment.Id);
        Assert.Equal(incidentId, assignment.IncidentId);
        Assert.Equal(createdAt, assignment.CreatedAt);
        Assert.Null(assignment.ClosedAt);
        Assert.False(assignment.IsClosed);
        Assert.Empty(assignment.HeroIds);
        Assert.Empty(assignment.DamageReportIds);
    }

    [Fact]
    public void MaxHeroes_IsThree()
    {
        Assert.Equal(3, Assignment.MaxHeroes);
    }

    [Fact]
    public void Create_EmptyIncidentId_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Assignment.Create(Guid.Empty, DateTime.UtcNow));

        Assert.Equal("validation.failed", exception.Code);
    }

    [Fact]
    public void AddHero_UpToLimit_AddsEachHero()
    {
        var assignment = CreateAssignment();
        var heroIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        foreach (var heroId in heroIds)
        {
            assignment.AddHero(heroId);
        }

        Assert.Equal(heroIds, assignment.HeroIds);
    }

    [Fact]
    public void AddHero_FourthHero_ThrowsAssignmentFull()
    {
        var assignment = CreateAssignment();
        assignment.AddHero(Guid.NewGuid());
        assignment.AddHero(Guid.NewGuid());
        assignment.AddHero(Guid.NewGuid());

        var exception = Assert.Throws<AssignmentFullException>(
            () => assignment.AddHero(Guid.NewGuid()));

        Assert.Equal("assignment.full", exception.Code);
    }

    [Fact]
    public void AddHero_DuplicateHero_ThrowsDuplicateAssignment()
    {
        var assignment = CreateAssignment();
        var heroId = Guid.NewGuid();
        assignment.AddHero(heroId);

        var exception = Assert.Throws<DuplicateAssignmentException>(() => assignment.AddHero(heroId));

        Assert.Equal("assignment.duplicate_hero", exception.Code);
    }

    [Fact]
    public void AddHero_EmptyHeroId_Throws()
    {
        var assignment = CreateAssignment();

        Assert.Throws<DomainValidationException>(() => assignment.AddHero(Guid.Empty));
    }

    [Fact]
    public void AddHero_AfterClose_ThrowsInvalidIncidentState()
    {
        var assignment = CreateAssignment();
        assignment.Close(DateTime.UtcNow);

        Assert.Throws<InvalidIncidentStateException>(() => assignment.AddHero(Guid.NewGuid()));
    }

    [Fact]
    public void AddDamageReport_RecordsReport()
    {
        var assignment = CreateAssignment();
        var reportId = Guid.NewGuid();

        assignment.AddDamageReport(reportId);

        Assert.Equal(reportId, Assert.Single(assignment.DamageReportIds));
    }

    [Fact]
    public void AddDamageReport_DuplicateReport_Throws()
    {
        var assignment = CreateAssignment();
        var reportId = Guid.NewGuid();
        assignment.AddDamageReport(reportId);

        Assert.Throws<DomainValidationException>(() => assignment.AddDamageReport(reportId));
    }

    [Fact]
    public void AddDamageReport_EmptyReportId_Throws()
    {
        var assignment = CreateAssignment();

        Assert.Throws<DomainValidationException>(() => assignment.AddDamageReport(Guid.Empty));
    }

    [Fact]
    public void AddDamageReport_AfterClose_ThrowsInvalidIncidentState()
    {
        var assignment = CreateAssignment();
        assignment.Close(DateTime.UtcNow);

        Assert.Throws<InvalidIncidentStateException>(
            () => assignment.AddDamageReport(Guid.NewGuid()));
    }

    [Fact]
    public void Close_OpenAssignment_SetsClosedAt()
    {
        var assignment = CreateAssignment();
        var closedAt = new DateTime(2026, 11, 11, 18, 0, 0, DateTimeKind.Utc);

        assignment.Close(closedAt);

        Assert.Equal(closedAt, assignment.ClosedAt);
        Assert.True(assignment.IsClosed);
    }

    [Fact]
    public void Close_Twice_ThrowsInvalidIncidentState()
    {
        var assignment = CreateAssignment();
        assignment.Close(DateTime.UtcNow);

        Assert.Throws<InvalidIncidentStateException>(() => assignment.Close(DateTime.UtcNow));
    }

    [Fact]
    public void Collections_ReturnedCollectionsAreReadOnly()
    {
        var assignment = CreateAssignment();
        assignment.AddHero(Guid.NewGuid());
        assignment.AddDamageReport(Guid.NewGuid());

        var heroIds = Assert.IsAssignableFrom<IList<Guid>>(assignment.HeroIds);
        var damageReportIds = Assert.IsAssignableFrom<IList<Guid>>(assignment.DamageReportIds);

        Assert.True(heroIds.IsReadOnly);
        Assert.True(damageReportIds.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => heroIds.Add(Guid.NewGuid()));
        Assert.Throws<NotSupportedException>(() => damageReportIds.Add(Guid.NewGuid()));
    }

    private static Assignment CreateAssignment() =>
        Assignment.Create(Guid.NewGuid(), DateTime.UtcNow);
}
