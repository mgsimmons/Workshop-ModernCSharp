namespace WWSHA.Domain;

public sealed class EntityNotFoundException(string entityName, Guid id)
    : DomainException("entity.not_found", $"{entityName} '{id}' was not found.");
