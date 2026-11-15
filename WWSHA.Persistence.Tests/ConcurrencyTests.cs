using WWSHA.Domain;
using WWSHA.Persistence;
using WWSHA.Persistence.Commands;
using Xunit;

namespace WWSHA.Persistence.Tests;

/// <summary>
/// Drives the store from many threads at once and checks that the business invariants still hold.
/// These tests catch lost updates and double-booking, which single-threaded tests cannot.
/// </summary>
public class ConcurrencyTests
{
    private static readonly DateTime Start = new(2026, 11, 15, 8, 0, 0);

    [Fact]
    public async Task Parallel_registration_and_filing_never_double_books_a_hero()
    {
        var store = new InMemoryDomainRepository();
        const int heroCount = 20;
        const int incidentCount = 30;

        var work = new List<Task>();
        for (int i = 0; i < heroCount; i++)
        {
            int n = i;
            work.Add(Task.Run(() => store.Execute(new RegisterHeroCommand($"Hero {n}", "x"), h => h)));
        }
        for (int i = 0; i < incidentCount; i++)
        {
            int n = i;
            work.Add(Task.Run(() => store.Execute(
                new FileIncidentCommand("Redmond", Start.AddMinutes(n), $"Incident {n}", IncidentCategory.PettyCrime),
                incident => incident)));
        }
        await Task.WhenAll(work);

        store.Query(ctx =>
        {
            Assert.Equal(heroCount, ctx.Heroes.Count);
            Assert.Equal(incidentCount, ctx.Incidents.Count);

            // Every hero appears in at most one assignment.
            var assignedHeroIds = ctx.Assignments.SelectMany(a => a.Heroes).Select(h => h.Id).ToList();
            Assert.Equal(assignedHeroIds.Count, assignedHeroIds.Distinct().Count());

            // Each assignment respects the three-hero limit.
            Assert.All(ctx.Assignments, a => Assert.InRange(a.Heroes.Count, 1, Assignment.MaxHeroes));

            // Demand (30) exceeds supply (20), so every hero is busy and the rest wait.
            Assert.All(ctx.Heroes, h => Assert.Equal(Availability.Unavailable, h.Availability));
            Assert.Equal(heroCount, assignedHeroIds.Count);
            Assert.Equal(incidentCount - heroCount, ctx.Incidents.Count(i => i.Status == IncidentStatus.Opened));

            // A hero's current incident matches the assignment they are on.
            foreach (var assignment in ctx.Assignments)
            {
                Assert.All(assignment.Heroes, h => Assert.Equal(assignment.Incident.Id, h.CurrentIncidentId));
            }

            return 0;
        });
    }

    [Fact]
    public async Task Parallel_closes_keep_reputation_and_assessments_consistent()
    {
        var store = new InMemoryDomainRepository();
        const int heroCount = 10;
        const int incidentCount = 25;

        for (int i = 0; i < heroCount; i++)
        {
            store.Execute(new RegisterHeroCommand($"Hero {i}", "x", Reputation: 0), h => h);
        }
        for (int i = 0; i < incidentCount; i++)
        {
            store.Execute(new FileIncidentCommand("Redmond", Start.AddMinutes(i), $"Incident {i}", IncidentCategory.PettyCrime),
                incident => incident);
        }

        // Close whatever is in progress, all at once. Each close frees heroes, which the command
        // dispatches to waiting incidents, so this runs in rounds until nothing is left in progress.
        int closedTotal = 0;
        while (true)
        {
            var inProgress = store.Query(ctx => ctx.Incidents
                .Where(i => i.Status == IncidentStatus.InProgress)
                .Select(i => i.Id)
                .ToArray());
            if (inProgress.Length == 0)
            {
                break;
            }

            var closes = inProgress.Select(id => Task.Run(() =>
                store.Execute(new CloseIncidentCommand(id), result => result)));
            await Task.WhenAll(closes);
            closedTotal += inProgress.Length;
        }

        store.Query(ctx =>
        {
            // Heroes were freed and dispatched until no incident was left waiting.
            Assert.Equal(incidentCount, ctx.Incidents.Count(i => i.Status == IncidentStatus.Closed));
            Assert.Equal(incidentCount, closedTotal);
            Assert.Equal(0, ctx.Incidents.Count(i => i.Status == IncidentStatus.Opened));
            Assert.All(ctx.Incidents, i => Assert.NotNull(i.Assignment));

            // Every hero is free again once nothing is in progress.
            Assert.All(ctx.Heroes, h => Assert.Equal(Availability.Available, h.Availability));

            // Reputation is exactly the sum of that hero's assessments, with no lost updates.
            foreach (var hero in ctx.Heroes)
            {
                Assert.Equal(hero.Assessments.Sum(a => a.ReputationChange), hero.Reputation);
            }

            // One assessment per hero per incident they worked, and nothing recorded twice.
            var pairs = ctx.Assessments.Select(a => (a.HeroId, a.IncidentId)).ToList();
            Assert.Equal(pairs.Count, pairs.Distinct().Count());
            Assert.All(ctx.Assessments, a =>
                Assert.Equal(IncidentStatus.Closed, ctx.Incidents.Single(i => i.Id == a.IncidentId).Status));

            return 0;
        });
    }
}
