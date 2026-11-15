using WWSHA.Persistence;

namespace WWSHA.Api.Services;

/// <summary>
/// The API's entry point to the store. The store takes its own lock for every command and query,
/// so requests from many threads never interleave. Projections to DTOs run inside that lock,
/// so a response never reflects a half-applied change.
/// </summary>
public sealed class DomainService(InMemoryDomainRepository store)
{
    public TResult Command<T, TResult>(ICommand<T> command, Func<T, TResult> project) =>
        store.Execute(command, project);

    public TResult Query<TResult>(Func<IDomainContext, TResult> query) =>
        store.Query(query);
}
