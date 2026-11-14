namespace WWSHA.Api.Contracts;

public sealed record IncidentDto(
    Guid Id,
    string City,
    DateTime OccurredAt,
    string Description,
    string Category,
    string Status,
    Guid? AssignmentId,
    int AssignedHeroCount,
    int DamageReportCount);
