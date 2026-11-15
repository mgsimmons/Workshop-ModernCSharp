namespace WWSHA.Domain;

public sealed class Hero
{
    private readonly List<Assessment> _assessments = [];

    public Hero(string name, string superpower, int reputation = 0)
    {
        Name = Check.Text(name, nameof(name), DomainLimits.NameMaxLength);
        Superpower = Check.Text(superpower, nameof(superpower), DomainLimits.SuperpowerMaxLength);
        Reputation = Check.Range(reputation, nameof(reputation),
            DomainLimits.StartingReputationMin, DomainLimits.StartingReputationMax);
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public string Superpower { get; }
    public Availability Availability { get; private set; } = Availability.Available;
    public int Reputation { get; private set; }

    /// <summary>The incident this hero is currently working, or null when available.</summary>
    public Guid? CurrentIncidentId { get; private set; }

    public IReadOnlyList<Assessment> Assessments => _assessments;

    /// <summary>Marks the hero as out on the given incident. Fails if they already are.</summary>
    public void Assign(Guid incidentId)
    {
        if (Availability == Availability.Unavailable)
            throw new HeroUnavailableException(Name);

        Availability = Availability.Unavailable;
        CurrentIncidentId = incidentId;
    }

    /// <summary>
    /// Records the agency's assessment for the incident the hero is working on, adjusts reputation,
    /// and makes the hero available again. The assessment must be for this hero and this incident.
    /// </summary>
    public void RecordAssessment(Assessment assessment)
    {
        ValidateAssessment(assessment);

        _assessments.Add(assessment);
        Reputation += assessment.ReputationChange;
        Availability = Availability.Available;
        CurrentIncidentId = null;
    }

    /// <summary>
    /// Checks that <paramref name="assessment"/> could be recorded, without recording it. Callers
    /// validate a whole batch before applying any of it, so a failure leaves nothing half-done.
    /// </summary>
    internal void ValidateAssessment(Assessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        if (Availability == Availability.Available || CurrentIncidentId is null)
            throw new InvalidAssessmentException($"Hero '{Name}' has no active assignment to assess.");

        if (assessment.HeroId != Id)
            throw new InvalidAssessmentException("The assessment does not belong to this hero.");

        if (assessment.IncidentId != CurrentIncidentId)
            throw new InvalidAssessmentException(
                $"The assessment is for incident '{assessment.IncidentId}', but hero '{Name}' is working on '{CurrentIncidentId}'.");
    }
}
