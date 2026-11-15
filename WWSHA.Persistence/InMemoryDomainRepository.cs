using WWSHA.Domain;

namespace WWSHA.Persistence;

/// <summary>
/// Stores every domain object in a <see cref="List{T}"/>. Implements both the read side
/// (<see cref="IDomainContext"/>) and the write side (<see cref="IDomainRepository"/>).
/// </summary>
public sealed class InMemoryDomainRepository : IDomainContext, IDomainRepository
{
    private readonly List<Hero> _heroes = [];
    private readonly List<Incident> _incidents = [];
    private readonly List<Assignment> _assignments = [];
    private readonly List<Report> _reports = [];
    private readonly List<Assessment> _assessments = [];

    public IReadOnlyCollection<Hero> Heroes => _heroes;
    public IReadOnlyCollection<Incident> Incidents => _incidents;
    public IReadOnlyCollection<Assignment> Assignments => _assignments;
    public IReadOnlyCollection<Report> Reports => _reports;
    public IReadOnlyCollection<Assessment> Assessments => _assessments;

    public void Save(Hero hero) => AddIfMissing(_heroes, hero);
    public void Save(Incident incident) => AddIfMissing(_incidents, incident);
    public void Save(Assignment assignment) => AddIfMissing(_assignments, assignment);
    public void Save(Report report) => AddIfMissing(_reports, report);
    public void Save(Assessment assessment) => AddIfMissing(_assessments, assessment);

    // Domain objects are mutable reference types and are updated in place, so "update" is
    // just "make sure it's in the list". Reference equality is what we want here.
    private static void AddIfMissing<T>(List<T> list, T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!list.Contains(item))
            list.Add(item);
    }
}
