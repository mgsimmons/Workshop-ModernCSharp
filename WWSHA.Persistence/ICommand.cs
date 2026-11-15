namespace WWSHA.Persistence;

/// <summary>A unit of work that changes domain state, using the context to read and the repository to write.</summary>
public interface ICommand
{
    void Execute(IDomainContext context, IDomainRepository repository);
}

/// <summary>A unit of work that changes domain state and returns a result.</summary>
public interface ICommand<TResult>
{
    TResult Execute(IDomainContext context, IDomainRepository repository);
}
