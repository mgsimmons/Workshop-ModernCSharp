using WWSHA.Domain;

namespace WWSHA.Api.Contracts;

public sealed record FileReportRequest(
    string Description,
    ReportSeverity Severity,
    int Cost,
    int Injury,
    DateTime? ReportedAt);
