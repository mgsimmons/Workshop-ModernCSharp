namespace WWSHA.Api.Infrastructure;

/// <summary>
/// Parses query-string enum values explicitly so invalid values produce a 400 instead of
/// silently matching nothing.
/// </summary>
internal static class EnumParsing
{
    internal static bool TryParseDefined<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);
}
