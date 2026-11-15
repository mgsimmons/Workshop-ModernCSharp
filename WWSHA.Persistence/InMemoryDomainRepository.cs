using WWSHA.Domain;

namespace WWSHA.Persistence;

/// <summary>
/// Stores every domain object in memory. Implements both the read side (<see cref="IDomainContext"/>)
/// and the write side (<see cref="IDomainRepository"/>).
/// </summary>
/// <remarks>
/// Every public member takes the same lock, so a command, a save, or a query never interleaves
/// with another. Reads return snapshot arrays, so callers can enumerate them safely. A snapshot is
/// rebuilt only after a new object is saved, so repeated reads between writes cost nothing extra.
/// Query callers that map domain objects to other shapes should do so inside <see cref="Query{TResult}"/>,
/// because the objects themselves are still mutated by commands.
/// </remarks>
public sealed class InMemoryDomainRepository : IDomainContext, IDomainRepository
{
    private readonly Lock _gate = new();
    private readonly Table<Hero> _heroes = new(h => h.Id);
    private readonly Table<Incident> _incidents = new(i => i.Id);
    private readonly Table<Assignment> _assignments = new(a => a.Id);
    private readonly Table<Report> _reports = new(r => r.Id);
    private readonly Table<Assessment> _assessments = new(a => a.Id);

    public InMemoryDomainRepository(TimeProvider? clock = null)
    {
        Clock = clock ?? TimeProvider.System;
    }

    public TimeProvider Clock { get; }

    public IReadOnlyCollection<Hero> Heroes { get { lock (_gate) return _heroes.Snapshot(); } }
    public IReadOnlyCollection<Incident> Incidents { get { lock (_gate) return _incidents.Snapshot(); } }
    public IReadOnlyCollection<Assignment> Assignments { get { lock (_gate) return _assignments.Snapshot(); } }
    public IReadOnlyCollection<Report> Reports { get { lock (_gate) return _reports.Snapshot(); } }
    public IReadOnlyCollection<Assessment> Assessments { get { lock (_gate) return _assessments.Snapshot(); } }

    public void Save(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        lock (_gate) _heroes.Save(hero);
    }

    public void Save(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        lock (_gate) _incidents.Save(incident);
    }

    public void Save(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        lock (_gate) _assignments.Save(assignment);
    }

    public void Save(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);
        lock (_gate) _reports.Save(report);
    }

    public void Save(Assessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        lock (_gate) _assessments.Save(assessment);
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

    /// <summary>
    /// Insertion-ordered storage for one kind of domain object. Membership is checked by id in
    /// constant time. Domain objects are mutable and updated in place, so "save" only has to make
    /// sure the object is present.
    /// </summary>
    private sealed class Table<T>(Func<T, Guid> keyOf) where T : class
    {
        private readonly List<T> _rows = [];
        private readonly HashSet<Guid> _keys = [];
        private T[]? _snapshot;

        public void Save(T item)
        {
            if (_keys.Add(keyOf(item)))
            {
                _rows.Add(item);
                _snapshot = null;
            }
        }

        public IReadOnlyCollection<T> Snapshot() => _snapshot ??= _rows.ToArray();
    }
}
