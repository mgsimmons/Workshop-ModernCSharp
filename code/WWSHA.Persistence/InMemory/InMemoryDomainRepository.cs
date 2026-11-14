using WWSHA.Domain;

using System.Linq;

namespace WWSHA.Persistence.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IDomainRepository"/> and <see cref="IDomainContext"/>.
/// Queries take a snapshot under a lock; command execution is serialized so writes never interleave.
/// A single instance serves both the read and write interfaces.
/// </summary>
public sealed class InMemoryDomainRepository : IDomainRepository, IDomainContext
{
    internal List<Hero> _Heroes { get; } = [];

    internal List<Incident> _Incidents { get; } = [];

    internal List<Assignment> _Assignments { get; } = [];

    internal List<Report> _Reports { get; } = [];

    private readonly object _sync = new();

    public IQueryable<Hero> Heroes
    {
        get { return Snapshot(_Heroes); }
    }

    public IQueryable<Incident> Incidents
    {
        get { return Snapshot(_Incidents); }
    }

    public IQueryable<Assignment> Assignments
    {
        get { return Snapshot(_Assignments); }
    }

    public IQueryable<Report> Reports
    {
        get { return Snapshot(_Reports); }
    }

    public IQueryable<Assessment> Assessments
    {
        get { return _Heroes.SelectMany(hero => hero.Assessments).ToArray().AsQueryable(); }
    }

    private IQueryable<T> Snapshot<T>(List<T> items)
    {
        lock (_sync)
        {
            return items.ToArray().AsQueryable();
        }
    }

    public void AddHero(Hero hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        _Heroes.Add(hero);
    }

    public void AddIncident(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        _Incidents.Add(incident);
    }

    public void AddAssignment(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        _Assignments.Add(assignment);
    }

    public void AddReport(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _Reports.Add(report);
    }

    public Task ExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            command.Execute(this, this);
        }

        return Task.CompletedTask;
    }

    public Task<TResult> ExecuteAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        TResult result;
        lock (_sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = command.Execute(this, this);
        }

        return Task.FromResult(result);
    }
}
