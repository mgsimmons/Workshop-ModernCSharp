using WWSHA.Domain;

namespace WWSHA.Persistence;

/// <summary>Read-only access to the domain objects. Use LINQ over these collections to query.</summary>
public interface IDomainContext
{
    IReadOnlyCollection<Hero> Heroes { get; }
    IReadOnlyCollection<Incident> Incidents { get; }
    IReadOnlyCollection<Assignment> Assignments { get; }
    IReadOnlyCollection<Report> Reports { get; }
    IReadOnlyCollection<Assessment> Assessments { get; }
}
