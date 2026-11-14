namespace WWSHA.Api.Contracts;

public sealed record HeroDto(
    Guid Id,
    string Name,
    string Superpower,
    string Availability,
    int Reputation,
    Guid? CurrentIncidentId,
    int AssessmentCount);
