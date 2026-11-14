namespace WWSHA.Api.Contracts;

public sealed record IncidentDetailDto(
    Guid Id,
    string City,
    DateTime OccurredAt,
    string Description,
    string Category,
    string Status,
    Guid? AssignmentId,
    int AssignedHeroCount,
    int DamageReportCount,
    AssignmentDto? Assignment,
    IReadOnlyList<ReportDto> Reports);
