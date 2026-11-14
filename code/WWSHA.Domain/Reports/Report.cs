namespace WWSHA.Domain;

public sealed class Report
{
    public required Guid Id { get; init; }
    public required Guid IncidentId { get; init; }
    public Guid? AssignmentId { get; private set; }
    public required string City { get; init; }
    public required string Description { get; init; }
    public required ReportSeverity Severity { get; init; }
    public required int Cost { get; init; }
    public required int Injury { get; init; }
    public required DateTime ReportedAt { get; init; }

    private Report()
    {
    }

    internal static Report File(
        Guid incidentId,
        string city,
        string description,
        ReportSeverity severity,
        int cost,
        int injury,
        DateTime reportedAt)
    {
        incidentId.RequireNonEmpty("An incident id is required for a report.");

        return new Report
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            City = city.RequireNonBlank("A report city is required."),
            Description = description.RequireNonBlank("A report description is required."),
            Severity = severity.RequireDefined("A valid report severity is required."),
            Cost = cost.RequireNonNegative("The report cost cannot be negative."),
            Injury = injury.RequireNonNegative("The injury count cannot be negative."),
            ReportedAt = reportedAt
        };
    }

    internal void AttachToAssignment(Guid assignmentId)
    {
        assignmentId.RequireNonEmpty("An assignment id is required.");

        if (AssignmentId is not null && AssignmentId != assignmentId)
        {
            throw new DomainValidationException(
                $"Report '{Id}' is already attached to assignment '{AssignmentId}'.");
        }

        AssignmentId = assignmentId;
    }
}
