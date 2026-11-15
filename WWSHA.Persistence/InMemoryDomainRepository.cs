using WWSHA.Domain;

namespace WWSHA.Persistence;

/// <summary>
/// Stores every domain object in a <see cref="List{T}"/>. Implements both the read side
/// (<see cref="IDomainContext"/>) and the write side (<see cref="IDomainRepository"/>).
/// </summary>
/// <remarks>
/// Every public member takes the same lock, so a command, a save, or a query never interleaves
/// with another. Reads return snapshot copies, so callers can enumerate them safely. Query
/// callers that map domain objects to other shapes should do so inside <see cref="Query{TResult}"/>,
/// because the objects themselves are still mutated by commands.
/// </remarks>
public sealed class InMemoryDomainRepository : IDomainContext, IDomainRepository
{
    private readonly Lock _gate = new();
    private readonly List<Hero> _heroes = [];
    private readonly List<Incident> _incidents = [];
    private readonly List<Assignment> _assignments = [];
    private readonly List<Report> _reports = [];
    private readonly List<Assessment> _assessments = [];

    public InMemoryDomainRepository(TimeProvider? clock = null)
    {
        Clock = clock ?? TimeProvider.System;
    }

    public TimeProvider Clock { get; }

    public IReadOnlyCollection<Hero> Heroes => Snapshot(_heroes);
    public IReadOnlyCollection<Incident> Incidents => Snapshot(_incidents);
    public IReadOnlyCollection<Assignment> Assignments => Snapshot(_assignments);
    public IReadOnlyCollection<Report> Reports => Snapshot(_reports);
    public IReadOnlyCollection<Assessment> Assessments => Snapshot(_assessments);

    public void Save(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        lock (_gate) AddIfMissing(_heroes, hero);
    }

    public void Save(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        lock (_gate) AddIfMissing(_incidents, incident);
    }

    public void Save(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        lock (_gate) AddIfMissing(_assignments, assignment);
    }

    public void Save(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);
        lock (_gate) AddIfMissing(_reports, report);
    }

    public void Save(Assessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        lock (_gate) AddIfMissing(_assessments, assessment);
    }

    /// <summary>
    /// Runs a command under the store lock and projects its result to the caller's shape before the
    /// lock is released. Use this from the API so responses never see a half-applied change.
    /// </summary>
    public TResult Execute<T, TResult>(ICommand<T> command, Func<T, TResult> project)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(project);

        lock (_gate)
        {
            return project(command.Execute(this, this));
        }
    }

    /// <summary>Runs a read under the store lock. Map domain objects to DTOs inside <paramref name="query"/>.</summary>
    public TResult Query<TResult>(Func<IDomainContext, TResult> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (_gate)
        {
            return query(this);
        }
    }

    private IReadOnlyCollection<T> Snapshot<T>(List<T> items)
    {
        lock (_gate)
        {
            return items.ToArray();
        }
    }

    // Domain objects are mutable reference types and are updated in place, so "update" is
    // just "make sure it's in the list". Reference equality is what we want here.
    private static void AddIfMissing<T>(List<T> list, T item) where T : class
    {
        if (!list.Contains(item))
            list.Add(item);
    }
}
