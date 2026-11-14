using WWSHA.Domain;
using WWSHA.Persistence.Internal;

namespace WWSHA.Persistence.Commands;

public sealed record CloseIncidentCommand(Guid IncidentId) : ICommand<CloseIncidentResult>
{
    public CloseIncidentResult Execute(IDomainRepository repository, IDomainContext context)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(context);

        var incident = context.GetIncident(IncidentId);
        if (incident.IsClosed)
        {
            throw new IncidentClosedException(incident.Id);
        }

        var assignment = context.FindAssignmentForIncident(incident.Id);
        if (assignment is null)
        {
            incident.Close();
            return new CloseIncidentResult(incident, []);
        }

        if (assignment.IsClosed)
        {
            throw new InvalidIncidentStateException($"Assignment '{assignment.Id}' is already closed.");
        }

        var heroes = new List<Hero>(assignment.HeroIds.Count);
        foreach (var heroId in assignment.HeroIds)
        {
            var hero = context.GetHero(heroId);
            if (hero.Availability != HeroAvailability.Assigned || hero.CurrentIncidentId != incident.Id)
            {
                throw new InvalidIncidentStateException(
                    $"Hero '{hero.Id}' is not assigned to incident '{incident.Id}'.");
            }

            heroes.Add(hero);
        }

        HeroDispatchService.AttachReports(context, incident, assignment);

        var damageCount = incident.ReportIds.Count;
        var closedAt = DateTime.Now;
        var assessments = new List<Assessment>(heroes.Count);

        foreach (var hero in heroes)
        {
            var assessment = Assessment.Create(hero.Id, incident.Id, damageCount, closedAt);
            hero.CompleteAssignment(assessment);
            assessments.Add(assessment);
        }

        assignment.Close(closedAt);
        incident.Close();
        HeroDispatchService.DispatchAvailableHeroes(repository, context);

        return new CloseIncidentResult(incident, assessments);
    }
}
