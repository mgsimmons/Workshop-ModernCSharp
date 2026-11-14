using System.Reflection;
using WWSHA.Domain;

namespace WWSHA.Domain.Tests;

public sealed class PublicSurfaceTests
{
    public static TheoryData<Type> EntityTypes => new()
    {
        typeof(Hero),
        typeof(Assessment),
        typeof(Incident),
        typeof(Assignment),
        typeof(Report)
    };

    [Theory]
    [MemberData(nameof(EntityTypes))]
    public void Entity_ExposesNoPublicMutatingMethods(Type entityType)
    {
        var publicMethods = entityType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();

        Assert.Empty(publicMethods);
    }

    [Theory]
    [MemberData(nameof(EntityTypes))]
    public void Entity_ExposesNoPublicConstructors(Type entityType)
    {
        Assert.Empty(entityType.GetConstructors());
    }
}
