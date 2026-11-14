namespace WWSHA.Domain;

public sealed class DuplicateAssignmentException(Guid heroId, Guid assignmentId)
    : DomainException("assignment.duplicate_hero", $"Hero '{heroId}' is already assigned to assignment '{assignmentId}'.");
