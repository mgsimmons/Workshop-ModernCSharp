using WWSHA.Domain;

namespace WWSHA.Api.Contracts;

// ---- Responses -------------------------------------------------------------

public sealed record HeroDto(
    Guid Id,
    string Name,
    string Superpower,
    Availability Availability,
    int Reputation);

public sealed record HeroDetailDto(
    Guid Id,
    string Name,
    string Superpower,
    Availability Availability,
    int Reputation,
    IReadOnlyList<AssessmentDto> Assessments);

public sealed record IncidentDto(
    Guid Id,
    string City,
    DateTime OccurredAt,
    string Description,
    IncidentCategory Category,
    IncidentStatus Status,
    Guid? AssignmentId);

public sealed record IncidentDetailDto(
    IncidentDto Incident,
    AssignmentDto? Assignment,
    IReadOnlyList<ReportDto> Reports);

public sealed record AssignmentDto(
    Guid Id,
    Guid IncidentId,
    IncidentStatus IncidentStatus,
    IReadOnlyList<HeroDto> Heroes,
    IReadOnlyList<ReportDto> Reports);

public sealed record ReportDto(
    Guid Id,
    Guid IncidentId,
    string City,
    string Description,
    DateTime FiledAt);

public sealed record AssessmentDto(
    Guid Id,
    Guid HeroId,
    Guid IncidentId,
    int ReportCount,
    int ReputationChange);

public sealed record CloseIncidentResponse(
    IncidentDto Incident,
    IReadOnlyList<AssessmentDto> Assessments);

// ---- Requests --------------------------------------------------------------

public sealed record RegisterHeroRequest(string Name, string Superpower, int Reputation = 0);

public sealed record FileIncidentRequest(
    string City,
    DateTime OccurredAt,
    string Description,
    IncidentCategory Category);

public sealed record AssignHeroRequest(Guid HeroId);

public sealed record FileReportRequest(string City, string Description, DateTime FiledAt);
