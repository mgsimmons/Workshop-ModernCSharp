namespace WWSHA.Domain;

public sealed class Assignment
{
    public const int MaxHeroes = 3;

    private readonly List<Guid> _heroIds = [];
    private readonly List<Guid> _damageReportIds = [];

    public required Guid Id { get; init; }
    public required Guid IncidentId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; private set; }

    public IReadOnlyList<Guid> HeroIds
    {
        get => field ??= _heroIds.AsReadOnly();
    }

    public IReadOnlyList<Guid> DamageReportIds
    {
        get => field ??= _damageReportIds.AsReadOnly();
    }

    public bool IsClosed => ClosedAt is not null;

    private Assignment()
    {
    }

    internal static Assignment Create(Guid incidentId, DateTime createdAt)
    {
        incidentId.RequireNonEmpty("An incident id is required for an assignment.");

        return new Assignment
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            CreatedAt = createdAt
        };
    }

    internal void AddHero(Guid heroId)
    {
        heroId.RequireNonEmpty("A hero id is required.");

        if (IsClosed)
        {
            throw new InvalidIncidentStateException($"Assignment '{Id}' is closed.");
        }

        if (_heroIds.Contains(heroId))
        {
            throw new DuplicateAssignmentException(heroId, Id);
        }

        if (_heroIds.Count >= MaxHeroes)
        {
            throw new AssignmentFullException(Id);
        }

        _heroIds.Add(heroId);
    }

    internal void AddDamageReport(Guid reportId)
    {
        reportId.RequireNonEmpty("A report id is required.");

        if (IsClosed)
        {
            throw new InvalidIncidentStateException($"Assignment '{Id}' is closed.");
        }

        if (_damageReportIds.Contains(reportId))
        {
            throw new DomainValidationException($"Report '{reportId}' is already in assignment '{Id}'.");
        }

        _damageReportIds.Add(reportId);
    }

    internal void Close(DateTime closedAt)
    {
        if (IsClosed)
        {
            throw new InvalidIncidentStateException($"Assignment '{Id}' is already closed.");
        }

        ClosedAt = closedAt;
    }
}
