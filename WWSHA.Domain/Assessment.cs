namespace WWSHA.Domain;

/// <summary>
/// The agency's final assessment of a hero's conduct on an incident.
/// Reputation goes up by <see cref="BaseReputationGain"/>, minus one per damage report filed.
/// </summary>
public sealed class Assessment
{
    public const int BaseReputationGain = 5;

    public Assessment(Guid heroId, Guid incidentId, int reportCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(reportCount);

        HeroId = heroId;
        IncidentId = incidentId;
        ReportCount = reportCount;
        ReputationChange = BaseReputationGain - reportCount;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Guid HeroId { get; }
    public Guid IncidentId { get; }
    public int ReportCount { get; }
    public int ReputationChange { get; }
}
