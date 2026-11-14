namespace WWSHA.Domain;

public sealed class Hero
{
    private readonly List<Assessment> _assessments = [];

    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Superpower { get; init; }
    public HeroAvailability Availability { get; private set; }
    public int Reputation { get; private set; }
    public Guid? CurrentIncidentId { get; private set; }

    public IReadOnlyList<Assessment> Assessments
    {
        get => field ??= _assessments.AsReadOnly();
    }

    private Hero()
    {
        Availability = HeroAvailability.Available;
    }

    internal static Hero Register(string name, string superpower)
    {
        return new Hero
        {
            Id = Guid.NewGuid(),
            Name = name.RequireNonBlank("A hero name is required."),
            Superpower = superpower.RequireNonBlank("A hero superpower is required.")
        };
    }

    internal void AssignTo(Guid incidentId)
    {
        incidentId.RequireNonEmpty("An incident id is required for assignment.");

        if (Availability != HeroAvailability.Available)
        {
            throw new HeroUnavailableException(Id);
        }

        Availability = HeroAvailability.Assigned;
        CurrentIncidentId = incidentId;
    }

    internal void CompleteAssignment(Assessment assessment)
    {
        if (assessment is null)
        {
            throw new DomainValidationException("An assessment is required.");
        }

        if (assessment.HeroId != Id)
        {
            throw new DomainValidationException("The assessment does not belong to this hero.");
        }

        if (Availability != HeroAvailability.Assigned)
        {
            throw new InvalidIncidentStateException($"Hero '{Id}' is not assigned and cannot be assessed.");
        }

        if (CurrentIncidentId != assessment.IncidentId)
        {
            throw new InvalidIncidentStateException(
                $"The assessment incident does not match hero '{Id}'s current assignment.");
        }

        _assessments.Add(assessment);
        Reputation += assessment.ReputationDelta;
        Availability = HeroAvailability.Available;
        CurrentIncidentId = null;
    }
}
