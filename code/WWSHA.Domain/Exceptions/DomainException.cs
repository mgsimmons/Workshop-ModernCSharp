namespace WWSHA.Domain;

public class DomainException(
    string code,
    string message,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}
