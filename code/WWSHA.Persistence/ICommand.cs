namespace WWSHA.Persistence;

/// <summary>
/// A synchronous domain operation. Commands are synchronous because the store is in-memory;
/// the repository exposes <see cref="Task"/>-returning methods for API ergonomics.
/// </summary>
public interface ICommand
{
    void Execute(IDomainRepository repository, IDomainContext context);
}

/// <summary>
/// A synchronous domain operation that returns a result.
/// </summary>
/// <typeparam name="TResult">The result produced by the command.</typeparam>
public interface ICommand<TResult>
{
    TResult Execute(IDomainRepository repository, IDomainContext context);
}
