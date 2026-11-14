namespace WWSHA.Api.Contracts;

public sealed record AssessmentDto(
    Guid Id,
    Guid HeroId,
    Guid IncidentId,
    int DamageReportCount,
    int ReputationDelta,
    DateTime CreatedAt);
