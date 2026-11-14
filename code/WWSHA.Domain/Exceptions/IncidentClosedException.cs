namespace WWSHA.Domain;

public sealed class IncidentClosedException(Guid incidentId)
    : DomainException("incident.closed", $"Incident '{incidentId}' is closed.");
