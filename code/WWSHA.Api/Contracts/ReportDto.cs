using WWSHA.Domain;

namespace WWSHA.Api.Contracts;

public sealed record ReportDto(
    Guid Id,
    Guid IncidentId,
    Guid? AssignmentId,
    string City,
    string Description,
    ReportSeverity Severity,
    int Cost,
    int Injury,
    DateTime ReportedAt);
