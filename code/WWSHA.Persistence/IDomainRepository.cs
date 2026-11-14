using WWSHA.Domain;

namespace WWSHA.Persistence;

/// <summary>
/// The write-only persistence port. Every create or update flows through a command object
/// executed by <see cref="ExecuteAsync(ICommand, CancellationToken)"/>; queries belong to
/// <see cref="IDomainContext"/>.
/// </summary>
public interface IDomainRepository
{
    Task ExecuteAsync(ICommand command, CancellationToken cancellationToken = default);

    Task<TResult> ExecuteAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);

    void AddHero(Hero hero);
    void AddIncident(Incident incident);
    void AddAssignment(Assignment assignment);
    void AddReport(Report report);
}
