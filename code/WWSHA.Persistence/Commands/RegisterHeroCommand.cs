using WWSHA.Domain;
using WWSHA.Persistence.Internal;

namespace WWSHA.Persistence.Commands;

public sealed record RegisterHeroCommand(string Name, string Superpower) : ICommand<Hero>
{
    public Hero Execute(IDomainRepository repository, IDomainContext context)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);

        var hero = Hero.Register(Name, Superpower);
        repository.AddHero(hero);
        HeroDispatchService.DispatchAvailableHeroes(repository, context);

        return hero;
    }
}
