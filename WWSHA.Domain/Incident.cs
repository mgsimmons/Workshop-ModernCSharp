namespace WWSHA.Domain;

/// <summary>
/// An incident moves Opened -> InProgress (once a hero is assigned) -> Closed (once assessed).
/// Only an InProgress incident can be closed: closing one nobody has worked would leave no
/// assessments to record, so the rule is enforced rather than left to callers.
/// </summary>
public sealed class Incident
{
    private readonly List<Report> _reports = [];

    public Incident(string city, DateTime occurredAt, string description, IncidentCategory category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        City = city;
        OccurredAt = occurredAt;
        Description = description;
        Category = category;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string City { get; }
    public DateTime OccurredAt { get; }
    public string Description { get; }
    public IncidentCategory Category { get; }
    public IncidentStatus Status { get; private set; } = IncidentStatus.Opened;

    /// <summary>Null until the first hero is assigned.</summary>
    public Assignment? Assignment { get; private set; }

    /// <summary>
    /// Every report filed against this incident, including reports filed before any hero was assigned.
    /// </summary>
    public IReadOnlyList<Report> Reports => _reports;

    /// <summary>
    /// Assigns a hero, creating the assignment on the first call. Reports filed while the incident
    /// was Opened are attached to the new assignment.
    /// </summary>
    public void AssignHero(Hero hero, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(hero);

        if (Status == IncidentStatus.Closed)
            throw new IncidentClosedException(Id);

        // Work on a local so a failed assignment (e.g. busy hero) leaves the incident unchanged.
        var assignment = Assignment ?? new Assignment(this, now);
        assignment.AddHero(hero);

        if (Assignment is null)
        {
            foreach (var report in _reports)
                assignment.AddReport(report);
        }

        Assignment = assignment;
        Status = IncidentStatus.InProgress;
    }

    /// <summary>
    /// Files a damage report. Allowed while the incident is Opened or InProgress, for this incident's city.
    /// </summary>
    public Report FileReport(
        string city,
        string description,
        ReportSeverity severity,
        int cost,
        int injury,
        DateTime filedAt)
    {
        if (Status == IncidentStatus.Closed)
            throw new IncidentClosedException(Id);

        if (!string.Equals(city, City, StringComparison.OrdinalIgnoreCase))
            throw new InvalidReportException(
                $"Report city '{city}' does not match incident city '{City}'.");

        var report = new Report(Id, city, description, severity, cost, injury, filedAt);
        _reports.Add(report);
        Assignment?.AddReport(report);
        return report;
    }

    /// <summary>
    /// Closes an in-progress incident, assesses every assigned hero (reputation change is
    /// 5 minus the incident's report count), and frees them for new work.
    /// </summary>
    public void Close(DateTime closedAt)
    {
        if (Status == IncidentStatus.Closed)
            throw new IncidentClosedException(Id);

        if (Status != IncidentStatus.InProgress || Assignment is null)
            throw new InvalidIncidentStateException(
                $"Only in-progress incidents can be closed (current status: {Status}).");

        int reportCount = _reports.Count;
        foreach (var hero in Assignment.Heroes)
        {
            hero.RecordAssessment(new Assessment(hero.Id, Id, reportCount, closedAt));
        }

        Assignment.Close(closedAt);
        Status = IncidentStatus.Closed;
    }
}
