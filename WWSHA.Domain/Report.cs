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
        if (!Enum.IsDefined(severity))
            throw new InvalidReportException($"'{severity}' is not a valid report severity.");

        IncidentId = incidentId;
        City = Check.Text(city, nameof(city), DomainLimits.CityMaxLength);
        Description = Check.Text(description, nameof(description), DomainLimits.DescriptionMaxLength);
        Severity = severity;
        Cost = Check.Range(cost, nameof(cost), 0, DomainLimits.CostMax);
        Injury = Check.Range(injury, nameof(injury), 0, DomainLimits.InjuryMax);
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
