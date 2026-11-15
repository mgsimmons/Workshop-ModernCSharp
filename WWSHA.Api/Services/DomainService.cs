using WWSHA.Persistence;

namespace WWSHA.Api.Services;

/// <summary>
/// Runs commands and queries against the shared in-memory store, one at a time.
/// The store's lists are not thread-safe, so every access goes through the same lock.
/// Results are projected to DTOs inside the lock so no caller sees a half-updated object.
/// </summary>
public sealed class DomainService(InMemoryDomainRepository store)
{
    private readonly Lock _gate = new();

    public TResult Command<T, TResult>(ICommand<T> command, Func<T, TResult> project)
    {
        lock (_gate)
        {
            return project(command.Execute(store, store));
        }
    }

    public TResult Query<TResult>(Func<IDomainContext, TResult> query)
    {
        lock (_gate)
        {
            return query(store);
        }
    }
}
