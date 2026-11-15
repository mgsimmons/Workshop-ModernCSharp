namespace WWSHA.Domain;

/// <summary>A damage report phoned in by a city official or citizen during an incident.</summary>
public sealed class Report
{
    internal Report(Guid incidentId, string city, string description, DateTime filedAt)
    {
        IncidentId = incidentId;
        City = city;
        Description = description;
        FiledAt = filedAt;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Guid IncidentId { get; }
    public string City { get; }
    public string Description { get; }
    public DateTime FiledAt { get; }
}
