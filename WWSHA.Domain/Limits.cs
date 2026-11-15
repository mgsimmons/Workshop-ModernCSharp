namespace WWSHA.Domain;

/// <summary>Input limits enforced by the domain on every value it accepts.</summary>
public static class DomainLimits
{
    public const int NameMaxLength = 100;
    public const int SuperpowerMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int DescriptionMaxLength = 2000;

    /// <summary>Reputation a hero can be registered with. Later changes come from assessments.</summary>
    public const int StartingReputationMin = -100;
    public const int StartingReputationMax = 100;

    public const int CostMax = 1_000_000_000;
    public const int InjuryMax = 1_000_000;
}

internal static class Check
{
    /// <summary>Requires non-blank text no longer than <paramref name="maxLength"/>.</summary>
    public static string Text(string? value, string name, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"{name} must be {maxLength} characters or fewer.", name);
        }
        return value;
    }

    /// <summary>Requires a value between <paramref name="min"/> and <paramref name="max"/>, inclusive.</summary>
    public static int Range(int value, string name, int min, int max)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be between {min} and {max}.");
        }
        return value;
    }
}
