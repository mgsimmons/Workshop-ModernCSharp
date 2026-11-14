namespace WWSHA.Api.Contracts;

public sealed record CloseIncidentResponse(
    IncidentDetailDto Incident,
    IReadOnlyList<AssessmentDto> Assessments);
