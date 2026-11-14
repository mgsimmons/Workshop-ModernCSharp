namespace WWSHA.Domain;

public sealed class InvalidIncidentStateException(string message)
    : DomainException("incident.invalid_state", message);
