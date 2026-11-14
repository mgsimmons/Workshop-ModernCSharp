namespace WWSHA.Domain;

public sealed class Assessment
{
    public required Guid Id { get; init; }
    public required Guid HeroId { get; init; }
    public required Guid IncidentId { get; init; }
    public required int DamageReportCount { get; init; }
    public required int ReputationDelta { get; init; }
    public required DateTime CreatedAt { get; init; }

    private Assessment()
    {
    }

    internal static Assessment Create(
        Guid heroId,
        Guid incidentId,
        int damageReportCount,
        DateTime createdAt)
    {
        heroId.RequireNonEmpty("A hero id is required for an assessment.");
        incidentId.RequireNonEmpty("An incident id is required for an assessment.");
        damageReportCount.RequireNonNegative("The damage report count cannot be negative.");

        return new Assessment
        {
            Id = Guid.NewGuid(),
            HeroId = heroId,
            IncidentId = incidentId,
            DamageReportCount = damageReportCount,
            ReputationDelta = 5 - damageReportCount,
            CreatedAt = createdAt
        };
    }
}
