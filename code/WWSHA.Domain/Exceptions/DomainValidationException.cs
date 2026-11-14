namespace WWSHA.Domain;

public sealed class DomainValidationException(string message)
    : DomainException("validation.failed", message);
