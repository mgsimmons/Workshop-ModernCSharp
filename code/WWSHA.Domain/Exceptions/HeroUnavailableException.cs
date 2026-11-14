namespace WWSHA.Domain;

public sealed class HeroUnavailableException(Guid heroId)
    : DomainException("hero.unavailable", $"Hero '{heroId}' is not available for assignment.");
