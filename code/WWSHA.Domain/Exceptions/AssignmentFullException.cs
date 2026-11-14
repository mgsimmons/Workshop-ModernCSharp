namespace WWSHA.Domain;

public sealed class AssignmentFullException(Guid assignmentId)
    : DomainException("assignment.full", $"Assignment '{assignmentId}' already has the maximum of three heroes.");
