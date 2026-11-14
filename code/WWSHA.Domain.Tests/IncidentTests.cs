using WWSHA.Domain;

namespace WWSHA.Domain.Tests;

public sealed class IncidentTests
{
    [Fact]
    public void File_ValidInput_CreatesOpenedIncident()
    {
        var occurredAt = new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc);

        var incident = Incident.File(
            "  Townsville  ",
            occurredAt,
            "  A very slow chase  ",
            IncidentCategory.PettyCrime);

        Assert.NotEqual(Guid.Empty, incident.Id);
        Assert.Equal("Townsville", incident.City);
        Assert.Equal(occurredAt, incident.OccurredAt);
        Assert.Equal("A very slow chase", incident.Description);
        Assert.Equal(IncidentCategory.PettyCrime, incident.Category);
        Assert.Equal(IncidentStatus.Opened, incident.Status);
        Assert.True(incident.IsOpen);
        Assert.False(incident.IsClosed);
        Assert.Null(incident.AssignmentId);
        Assert.Empty(incident.ReportIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void File_BlankCity_Throws(string? city)
    {
        Assert.Throws<DomainValidationException>(
            () => Incident.File(city!, DateTime.UtcNow, "Description", IncidentCategory.PettyCrime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void File_BlankDescription_Throws(string? description)
    {
        Assert.Throws<DomainValidationException>(
            () => Incident.File("Townsville", DateTime.UtcNow, description!, IncidentCategory.PettyCrime));
    }

    [Fact]
    public void File_UndefinedCategory_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => Incident.File("Townsville", DateTime.UtcNow, "Description", (IncidentCategory)999));
    }

    [Fact]
    public void AttachAssignment_MovesIncidentToInProgress()
    {
        var incident = CreateIncident();
        var assignmentId = Guid.NewGuid();

        incident.AttachAssignment(assignmentId);

        Assert.Equal(assignmentId, incident.AssignmentId);
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.True(incident.IsOpen);
    }

    [Fact]
    public void AttachAssignment_WhenAlreadyAssigned_Throws()
    {
        var incident = CreateIncident();
        incident.AttachAssignment(Guid.NewGuid());

        var exception = Assert.Throws<InvalidIncidentStateException>(
            () => incident.AttachAssignment(Guid.NewGuid()));

        Assert.Equal("incident.invalid_state", exception.Code);
    }

    [Fact]
    public void AttachAssignment_WhenClosed_ThrowsIncidentClosed()
    {
        var incident = CreateIncident();
        incident.Close();

        var exception = Assert.Throws<IncidentClosedException>(
            () => incident.AttachAssignment(Guid.NewGuid()));

        Assert.Equal("incident.closed", exception.Code);
    }

    [Fact]
    public void AttachAssignment_EmptyAssignmentId_Throws()
    {
        var incident = CreateIncident();

        Assert.Throws<DomainValidationException>(() => incident.AttachAssignment(Guid.Empty));
    }

    [Fact]
    public void AddReport_BeforeAssignment_RecordsReport()
    {
        var incident = CreateIncident();
        var reportId = Guid.NewGuid();

        incident.AddReport(reportId);

        Assert.Equal(IncidentStatus.Opened, incident.Status);
        Assert.Equal(reportId, Assert.Single(incident.ReportIds));
    }

    [Fact]
    public void AddReport_DuplicateReport_Throws()
    {
        var incident = CreateIncident();
        var reportId = Guid.NewGuid();
        incident.AddReport(reportId);

        Assert.Throws<DomainValidationException>(() => incident.AddReport(reportId));
    }

    [Fact]
    public void AddReport_EmptyReportId_Throws()
    {
        var incident = CreateIncident();

        Assert.Throws<DomainValidationException>(() => incident.AddReport(Guid.Empty));
    }

    [Fact]
    public void AddReport_AfterClose_ThrowsIncidentClosed()
    {
        var incident = CreateIncident();
        incident.Close();

        var exception = Assert.Throws<IncidentClosedException>(() => incident.AddReport(Guid.NewGuid()));

        Assert.Equal("incident.closed", exception.Code);
    }

    [Fact]
    public void Close_OpenedIncident_MovesToClosed()
    {
        var incident = CreateIncident();

        incident.Close();

        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.True(incident.IsClosed);
        Assert.False(incident.IsOpen);
    }

    [Fact]
    public void Close_Twice_ThrowsIncidentClosed()
    {
        var incident = CreateIncident();
        incident.Close();

        Assert.Throws<IncidentClosedException>(() => incident.Close());
    }

    [Fact]
    public void ReportIds_ReturnedCollectionIsReadOnly()
    {
        var incident = CreateIncident();
        var reportIds = Assert.IsAssignableFrom<IList<Guid>>(incident.ReportIds);

        Assert.True(reportIds.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => reportIds.Add(Guid.NewGuid()));
    }

    private static Incident CreateIncident() =>
        Incident.File("Townsville", DateTime.UtcNow, "A very slow chase", IncidentCategory.PettyCrime);
}
