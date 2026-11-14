using WWSHA.Domain;

namespace WWSHA.Persistence;

/// <summary>
/// The outcome of closing an incident: the closed incident and the assessments created
/// for the heroes that worked it.
/// </summary>
public sealed record CloseIncidentResult(
    Incident Incident,
    IReadOnlyList<Assessment> Assessments);
