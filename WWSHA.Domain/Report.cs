namespace WWSHA.Domain;

/// <summary>A damage report phoned in by a city official or citizen during an incident.</summary>
public sealed class Report
{
    internal Report(
        Guid incidentId,
        string city,
        string description,
        ReportSeverity severity,
        int cost,
        int injury,
        DateTime filedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (!Enum.IsDefined(severity))
            throw new InvalidReportException($"'{severity}' is not a valid report severity.");
        ArgumentOutOfRangeException.ThrowIfNegative(cost);
        ArgumentOutOfRangeException.ThrowIfNegative(injury);

        IncidentId = incidentId;
        City = city;
        Description = description;
        Severity = severity;
        Cost = cost;
        Injury = injury;
        FiledAt = filedAt;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Guid IncidentId { get; }
    public string City { get; }
    public string Description { get; }
    public ReportSeverity Severity { get; }

    /// <summary>Estimated cost of the damage, in whole currency units.</summary>
    public int Cost { get; }

    /// <summary>Number of people injured.</summary>
    public int Injury { get; }

    public DateTime FiledAt { get; }
}
