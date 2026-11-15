namespace WWSHA.Domain;

/// <summary>
/// Base type for all rule violations raised by the domain model. <see cref="Code"/> is a stable,
/// machine-readable identifier that the API returns to clients alongside the message.
/// </summary>
public class DomainException(string message, string code = "domain.invalid") : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>A hero was assigned while already out on another assignment.</summary>
public sealed class HeroUnavailableException(string heroName)
    : DomainException($"Hero '{heroName}' is unavailable and cannot be assigned.", "hero.unavailable");

/// <summary>An assignment already has the maximum number of heroes.</summary>
public sealed class AssignmentFullException(int maxHeroes)
    : DomainException($"An assignment can have at most {maxHeroes} heroes.", "assignment.full");

/// <summary>The same hero was added to an assignment twice.</summary>
public sealed class DuplicateAssignmentException(string heroName)
    : DomainException($"Hero '{heroName}' is already on this assignment.", "assignment.duplicate");

/// <summary>An operation was attempted on an incident that is already closed.</summary>
public sealed class IncidentClosedException(Guid incidentId)
    : DomainException($"Incident '{incidentId}' is closed.", "incident.closed");

/// <summary>An operation was attempted in an incident status that does not allow it.</summary>
public sealed class InvalidIncidentStateException(string message)
    : DomainException(message, "incident.state");

/// <summary>A report does not belong to the incident it was filed against, or has invalid values.</summary>
public sealed class InvalidReportException(string message)
    : DomainException(message, "report.invalid");

/// <summary>An assessment does not match the hero or incident it is being recorded against.</summary>
public sealed class InvalidAssessmentException(string message)
    : DomainException(message, "assessment.invalid");
