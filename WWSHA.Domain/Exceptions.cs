namespace WWSHA.Domain;

/// <summary>Base type for all rule violations raised by the domain model.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>A hero was assigned while already out on another assignment.</summary>
public sealed class HeroUnavailableException(string heroName)
    : DomainException($"Hero '{heroName}' is unavailable and cannot be assigned.");

/// <summary>An incident already has the maximum number of heroes assigned.</summary>
public sealed class AssignmentFullException(int maxHeroes)
    : DomainException($"An assignment can have at most {maxHeroes} heroes.");

/// <summary>An operation was attempted in an incident status that does not allow it.</summary>
public sealed class InvalidIncidentStateException(string message) : DomainException(message);

/// <summary>A report does not belong to the incident it was filed against.</summary>
public sealed class InvalidReportException(string message) : DomainException(message);
