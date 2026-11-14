using WWSHA.Domain;

namespace WWSHA.Domain.Tests;

public sealed class ReportTests
{
    [Fact]
    public void File_ValidInput_StoresAllFields()
    {
        var incidentId = Guid.NewGuid();
        var reportedAt = new DateTime(2026, 11, 11, 8, 15, 0, DateTimeKind.Utc);

        var report = Report.File(
            incidentId,
            "  Townsville  ",
            "  A parked car was mildly inconvenienced  ",
            ReportSeverity.Moderate,
            1500,
            1,
            reportedAt);

        Assert.NotEqual(Guid.Empty, report.Id);
        Assert.Equal(incidentId, report.IncidentId);
        Assert.Null(report.AssignmentId);
        Assert.Equal("Townsville", report.City);
        Assert.Equal("A parked car was mildly inconvenienced", report.Description);
        Assert.Equal(ReportSeverity.Moderate, report.Severity);
        Assert.Equal(1500, report.Cost);
        Assert.Equal(1, report.Injury);
        Assert.Equal(reportedAt, report.ReportedAt);
    }

    [Theory]
    [InlineData(ReportSeverity.Minor)]
    [InlineData(ReportSeverity.Moderate)]
    [InlineData(ReportSeverity.Severe)]
    [InlineData(ReportSeverity.Catastrophic)]
    public void File_DefinedSeverity_IsAccepted(ReportSeverity severity)
    {
        var report = Report.File(
            Guid.NewGuid(),
            "Townsville",
            "Description",
            severity,
            0,
            0,
            DateTime.UtcNow);

        Assert.Equal(severity, report.Severity);
    }

    [Fact]
    public void File_EmptyIncidentId_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => Report.File(
                Guid.Empty,
                "Townsville",
                "Description",
                ReportSeverity.Minor,
                0,
                0,
                DateTime.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void File_BlankCity_Throws(string? city)
    {
        Assert.Throws<DomainValidationException>(
            () => Report.File(
                Guid.NewGuid(),
                city!,
                "Description",
                ReportSeverity.Minor,
                0,
                0,
                DateTime.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void File_BlankDescription_Throws(string? description)
    {
        Assert.Throws<DomainValidationException>(
            () => Report.File(
                Guid.NewGuid(),
                "Townsville",
                description!,
                ReportSeverity.Minor,
                0,
                0,
                DateTime.UtcNow));
    }

    [Fact]
    public void File_NegativeCost_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => Report.File(
                Guid.NewGuid(),
                "Townsville",
                "Description",
                ReportSeverity.Minor,
                -1,
                0,
                DateTime.UtcNow));
    }

    [Fact]
    public void File_NegativeInjury_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => Report.File(
                Guid.NewGuid(),
                "Townsville",
                "Description",
                ReportSeverity.Minor,
                0,
                -1,
                DateTime.UtcNow));
    }

    [Fact]
    public void File_UndefinedSeverity_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => Report.File(
                Guid.NewGuid(),
                "Townsville",
                "Description",
                (ReportSeverity)999,
                0,
                0,
                DateTime.UtcNow));
    }

    [Fact]
    public void AttachToAssignment_SetsAssignmentId()
    {
        var report = CreateReport();
        var assignmentId = Guid.NewGuid();

        report.AttachToAssignment(assignmentId);

        Assert.Equal(assignmentId, report.AssignmentId);
    }

    [Fact]
    public void AttachToAssignment_SameAssignmentTwice_IsIdempotent()
    {
        var report = CreateReport();
        var assignmentId = Guid.NewGuid();
        report.AttachToAssignment(assignmentId);

        report.AttachToAssignment(assignmentId);

        Assert.Equal(assignmentId, report.AssignmentId);
    }

    [Fact]
    public void AttachToAssignment_DifferentAssignment_Throws()
    {
        var report = CreateReport();
        report.AttachToAssignment(Guid.NewGuid());

        Assert.Throws<DomainValidationException>(() => report.AttachToAssignment(Guid.NewGuid()));
    }

    [Fact]
    public void AttachToAssignment_EmptyAssignmentId_Throws()
    {
        var report = CreateReport();

        Assert.Throws<DomainValidationException>(() => report.AttachToAssignment(Guid.Empty));
    }

    private static Report CreateReport() =>
        Report.File(
            Guid.NewGuid(),
            "Townsville",
            "A parked car was mildly inconvenienced",
            ReportSeverity.Minor,
            100,
            0,
            DateTime.UtcNow);
}
