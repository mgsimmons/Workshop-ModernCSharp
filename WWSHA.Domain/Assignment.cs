namespace WWSHA.Domain;

/// <summary>
/// Created when the first hero is assigned to an incident. Holds up to
/// <see cref="MaxHeroes"/> heroes and the damage history for the incident.
/// </summary>
public sealed class Assignment
{
    public const int MaxHeroes = 3;

    private readonly List<Hero> _heroes = [];
    private readonly List<Report> _reports = [];

    internal Assignment(Incident incident, DateTime createdAt)
    {
        Incident = incident;
        CreatedAt = createdAt;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Incident Incident { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ClosedAt { get; private set; }
    public bool IsClosed => ClosedAt is not null;
    public IReadOnlyList<Hero> Heroes => _heroes;
    public IReadOnlyList<Report> Reports => _reports;

    internal void AddHero(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);

        if (IsClosed)
            throw new InvalidIncidentStateException($"Assignment '{Id}' is closed.");

        if (_heroes.Contains(hero))
            throw new DuplicateAssignmentException(hero.Name);

        if (_heroes.Count >= MaxHeroes)
            throw new AssignmentFullException(MaxHeroes);

        hero.Assign(Incident.Id); // throws HeroUnavailableException before we mutate anything
        _heroes.Add(hero);
    }

    internal void AddReport(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (IsClosed)
            throw new InvalidIncidentStateException($"Assignment '{Id}' is closed.");

        if (_reports.Any(existing => existing.Id == report.Id))
            throw new InvalidReportException($"Report '{report.Id}' is already in this assignment.");

        _reports.Add(report);
    }

    internal void Close(DateTime closedAt)
    {
        if (IsClosed)
            throw new InvalidIncidentStateException($"Assignment '{Id}' is already closed.");

        ClosedAt = closedAt;
    }
}
