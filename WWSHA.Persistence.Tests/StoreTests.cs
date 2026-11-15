using WWSHA.Domain;
using WWSHA.Persistence;
using WWSHA.Persistence.Commands;
using Xunit;

namespace WWSHA.Persistence.Tests;

/// <summary>A clock that always fails, to test what happens when the time cannot be read.</summary>
internal sealed class BrokenClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("clock is down");
}

public class StoreTests
{
    [Fact]
    public async Task A_query_holds_the_lock_so_saves_wait_for_it()
    {
        var store = new InMemoryDomainRepository();
        using var inside = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        var query = Task.Run(() => store.Query(ctx =>
        {
            inside.Set();
            release.Wait(TimeSpan.FromSeconds(5));
            return ctx.Heroes.Count;
        }));
        Assert.True(inside.Wait(TimeSpan.FromSeconds(5)));

        var save = Task.Run(() => store.Save(new Hero("Blocked", "x")));

        // Give the save time to run. It cannot finish while the query still holds the lock.
        await Task.Delay(200);
        Assert.False(save.IsCompleted);

        release.Set();
        await Task.WhenAll(query, save);
        Assert.Single(store.Heroes);
    }

    [Fact]
    public void Snapshots_are_reused_between_writes_and_rebuilt_after_one()
    {
        var store = new InMemoryDomainRepository();
        var first = store.Heroes;

        Assert.Same(first, store.Heroes);

        store.Save(new Hero("A", "x"));

        Assert.NotSame(first, store.Heroes);
        Assert.Single(store.Heroes);
    }

    [Fact]
    public void Saving_the_same_object_twice_does_not_rebuild_the_snapshot()
    {
        var store = new InMemoryDomainRepository();
        var hero = new Hero("A", "x");
        store.Save(hero);
        var snapshot = store.Heroes;

        store.Save(hero);

        Assert.Same(snapshot, store.Heroes);
    }

    [Fact]
    public void A_clock_failure_leaves_no_partial_change()
    {
        var store = new InMemoryDomainRepository(new BrokenClock());

        Assert.Throws<InvalidOperationException>(() =>
            new FileIncidentCommand("Redmond", new DateTime(2026, 11, 15, 12, 0, 0), "Brawl",
                IncidentCategory.DrunkenBarFight).Execute(store, store));

        Assert.Empty(store.Incidents);
        Assert.Empty(store.Assignments);
    }

    [Fact]
    public void Incidents_more_than_a_day_in_the_future_are_rejected_and_nothing_is_saved()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));
        var store = new InMemoryDomainRepository(clock);
        var now = clock.Now.LocalDateTime;

        var ex = Assert.Throws<InvalidIncidentException>(() =>
            new FileIncidentCommand("Redmond", now.AddDays(2), "Future", IncidentCategory.PettyCrime)
                .Execute(store, store));

        Assert.Equal("incident.invalid", ex.Code);
        Assert.Empty(store.Incidents);
    }

    [Fact]
    public void Incidents_within_the_future_tolerance_are_accepted()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));
        var store = new InMemoryDomainRepository(clock);
        var now = clock.Now.LocalDateTime;

        var incident = new FileIncidentCommand("Redmond", now.AddHours(12), "Soon", IncidentCategory.PettyCrime)
            .Execute(store, store);

        Assert.Same(incident, Assert.Single(store.Incidents));
    }
}
