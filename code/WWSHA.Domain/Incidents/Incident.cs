namespace WWSHA.Domain;

public sealed class Incident
{
    private readonly List<Guid> _reportIds = [];

    public required Guid Id { get; init; }
    public required string City { get; init; }
    public required DateTime OccurredAt { get; init; }
    public required string Description { get; init; }
    public required IncidentCategory Category { get; init; }
    public IncidentStatus Status { get; private set; } = IncidentStatus.Opened;
    public Guid? AssignmentId { get; private set; }

    public IReadOnlyList<Guid> ReportIds
    {
        get => field ??= _reportIds.AsReadOnly();
    }

    public bool IsClosed => Status == IncidentStatus.Closed;
    public bool IsOpen => !IsClosed;

    private Incident()
    {
    }

    internal static Incident File(
        string city,
        DateTime occurredAt,
        string description,
        IncidentCategory category)
    {
        return new Incident
        {
            Id = Guid.NewGuid(),
            City = city.RequireNonBlank("An incident city is required."),
            OccurredAt = occurredAt,
            Description = description.RequireNonBlank("An incident description is required."),
            Category = category.RequireDefined("A valid incident category is required.")
        };
    }

    internal void AttachAssignment(Guid assignmentId)
    {
        assignmentId.RequireNonEmpty("An assignment id is required.");

        if (Status == IncidentStatus.Closed)
        {
            throw new IncidentClosedException(Id);
        }

        if (AssignmentId is not null)
        {
            throw new InvalidIncidentStateException($"Incident '{Id}' already has an assignment.");
        }

        AssignmentId = assignmentId;
        Status = IncidentStatus.InProgress;
    }

    internal void AddReport(Guid reportId)
    {
        reportId.RequireNonEmpty("A report id is required.");

        if (Status == IncidentStatus.Closed)
        {
            throw new IncidentClosedException(Id);
        }

        if (_reportIds.Contains(reportId))
        {
            throw new DomainValidationException($"Report '{reportId}' is already recorded on incident '{Id}'.");
        }

        _reportIds.Add(reportId);
    }

    internal void Close()
    {
        if (Status == IncidentStatus.Closed)
        {
            throw new IncidentClosedException(Id);
        }

        Status = IncidentStatus.Closed;
    }
}
