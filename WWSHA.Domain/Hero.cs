namespace WWSHA.Domain;

public sealed class Hero
{
    private readonly List<Assessment> _assessments = [];

    public Hero(string name, string superpower, int reputation = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(superpower);

        Name = name;
        Superpower = superpower;
        Reputation = reputation;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public string Superpower { get; }
    public Availability Availability { get; private set; } = Availability.Available;
    public int Reputation { get; private set; }
    public IReadOnlyList<Assessment> Assessments => _assessments;

    /// <summary>Marks the hero as out on assignment. Fails if they already are.</summary>
    public void Assign()
    {
        if (Availability == Availability.Unavailable)
            throw new HeroUnavailableException(Name);

        Availability = Availability.Unavailable;
    }

    /// <summary>Records the agency's assessment, adjusts reputation, and makes the hero available again.</summary>
    public void RecordAssessment(Assessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        if (Availability == Availability.Available)
            throw new DomainException($"Hero '{Name}' has no active assignment to assess.");

        _assessments.Add(assessment);
        Reputation += assessment.ReputationChange;
        Availability = Availability.Available;
    }
}
