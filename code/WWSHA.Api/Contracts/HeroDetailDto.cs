namespace WWSHA.Api.Contracts;

public sealed record HeroDetailDto(
    Guid Id,
    string Name,
    string Superpower,
    string Availability,
    int Reputation,
    Guid? CurrentIncidentId,
    IReadOnlyList<AssessmentDto> Assessments);
