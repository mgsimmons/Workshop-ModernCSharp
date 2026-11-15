namespace WWSHA.Domain;

public enum Availability
{
    Available,
    Unavailable
}

public enum IncidentStatus
{
    Opened,
    InProgress,
    Closed
}

public enum IncidentCategory
{
    PettyCrime,
    AlienInvasion,
    SupervillainAppearance,
    DisagreementBetweenHeros,
    DrunkenBarFight,
    WanderingKaiju
}

public enum ReportSeverity
{
    Minor,
    Moderate,
    Severe
}
