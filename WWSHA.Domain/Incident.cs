namespace WWSHA.Domain;

/// <summary>
/// An incident moves Opened -> InProgress (once a hero is assigned) -> Closed (once assessed).
/// </summary>
public sealed class Incident
{
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

    /// <summary>Assigns a hero, creating the assignment on the first call.</summary>
    public void AssignHero(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);

        if (Status == IncidentStatus.Closed)
            throw new InvalidIncidentStateException("Cannot assign heroes to a closed incident.");

        // Work on a local so a failed assignment (e.g. busy hero) leaves the incident unchanged.
        var assignment = Assignment ?? new Assignment(this);
        assignment.AddHero(hero);

        Assignment = assignment;
        Status = IncidentStatus.InProgress;
    }

    /// <summary>Files a damage report. Only valid while in progress, and only for this incident's city.</summary>
    public Report FileReport(string city, string description, DateTime filedAt)
    {
        if (Status != IncidentStatus.InProgress || Assignment is null)
            throw new InvalidIncidentStateException(
                $"Reports can only be filed while the incident is in progress (current status: {Status}).");

        if (!string.Equals(city, City, StringComparison.OrdinalIgnoreCase))
            throw new InvalidReportException(
                $"Report city '{city}' does not match incident city '{City}'.");

        var report = new Report(Id, city, description, filedAt);
        Assignment.AddReport(report);
        return report;
    }

    /// <summary>Closes the incident and assesses every assigned hero, freeing them for new work.</summary>
    public void Close()
    {
        if (Status != IncidentStatus.InProgress || Assignment is null)
            throw new InvalidIncidentStateException(
                $"Only in-progress incidents can be closed (current status: {Status}).");

        int reportCount = Assignment.Reports.Count;
        foreach (var hero in Assignment.Heroes)
        {
            hero.RecordAssessment(new Assessment(hero.Id, Id, reportCount));
        }

        Status = IncidentStatus.Closed;
    }
}
