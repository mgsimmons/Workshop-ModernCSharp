using WWSHA.Domain;

namespace WWSHA.Api.Contracts;

public sealed record FileIncidentRequest(
    string City,
    DateTime OccurredAt,
    string Description,
    IncidentCategory Category);
