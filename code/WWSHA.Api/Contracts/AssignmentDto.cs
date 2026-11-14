namespace WWSHA.Api.Contracts;

public sealed record AssignmentDto(
    Guid Id,
    Guid IncidentId,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    IReadOnlyList<HeroDto> Heroes,
    IReadOnlyList<ReportDto> DamageReports);
