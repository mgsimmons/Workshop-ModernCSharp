using WWSHA.Domain;

namespace WWSHA.Persistence;

/// <summary>
/// Write-only access to the domain objects. Each method inserts the object if it is new,
/// or updates it if it is already stored (saving the same object twice is harmless).
/// </summary>
public interface IDomainRepository
{
    void Save(Hero hero);
    void Save(Incident incident);
    void Save(Assignment assignment);
    void Save(Report report);
    void Save(Assessment assessment);
}
