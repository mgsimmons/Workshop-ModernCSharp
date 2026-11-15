namespace WWSHA.Domain;

/// <summary>
/// Created when the first hero is assigned to an incident. Holds up to
/// <see cref="MaxHeroes"/> heroes and the damage history for the incident.
/// </summary>
public sealed class Assignment
{
    public const int MaxHeroes = 3;

    private readonly List<Hero> _heroes = [];
    private readonly List<Report> _reports = [];

    internal Assignment(Incident incident)
    {
        Incident = incident;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Incident Incident { get; }
    public IReadOnlyList<Hero> Heroes => _heroes;
    public IReadOnlyList<Report> Reports => _reports;

    internal void AddHero(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);

        if (_heroes.Count >= MaxHeroes)
            throw new AssignmentFullException(MaxHeroes);

        hero.Assign(); // throws HeroUnavailableException before we mutate anything
        _heroes.Add(hero);
    }

    internal void AddReport(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _reports.Add(report);
    }
}
