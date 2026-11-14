namespace WWSHA.Domain;

internal static class DomainValidationExtensions
{
    extension(string? value)
    {
        internal string RequireNonBlank(string message)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrWhiteSpace(trimmed)
                ? throw new DomainValidationException(message)
                : trimmed;
        }
    }

    extension(Guid value)
    {
        internal Guid RequireNonEmpty(string message)
        {
            return value == Guid.Empty
                ? throw new DomainValidationException(message)
                : value;
        }
    }

    extension(int value)
    {
        internal int RequireNonNegative(string message)
        {
            return value < 0
                ? throw new DomainValidationException(message)
                : value;
        }
    }

    extension<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        internal TEnum RequireDefined(string message)
        {
            return Enum.IsDefined(value)
                ? value
                : throw new DomainValidationException(message);
        }
    }
}
