using WWSHA.Domain;

namespace WWSHA.Domain.Tests;

public sealed class DomainExceptionTests
{
    [Fact]
    public void DomainException_StoresCodeAndMessage()
    {
        var exception = new DomainException("some.code", "Something went wrong.");

        Assert.Equal("some.code", exception.Code);
        Assert.Equal("Something went wrong.", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void DomainException_StoresInnerException()
    {
        var inner = new InvalidOperationException("Inner failure.");
        var exception = new DomainException("some.code", "Something went wrong.", inner);

        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void SpecificExceptions_ExposeStableCodes()
    {
        Assert.Equal("validation.failed", new DomainValidationException("Message").Code);
        Assert.Equal("entity.not_found", new EntityNotFoundException("Hero", Guid.NewGuid()).Code);
        Assert.Equal("hero.unavailable", new HeroUnavailableException(Guid.NewGuid()).Code);
        Assert.Equal("incident.closed", new IncidentClosedException(Guid.NewGuid()).Code);
        Assert.Equal("assignment.full", new AssignmentFullException(Guid.NewGuid()).Code);
        Assert.Equal("assignment.duplicate_hero", new DuplicateAssignmentException(Guid.NewGuid(), Guid.NewGuid()).Code);
        Assert.Equal("incident.invalid_state", new InvalidIncidentStateException("Message").Code);
    }

    [Fact]
    public void SpecificExceptions_DeriveFromDomainException()
    {
        Assert.IsAssignableFrom<DomainException>(new DomainValidationException("Message"));
        Assert.IsAssignableFrom<DomainException>(new EntityNotFoundException("Hero", Guid.NewGuid()));
        Assert.IsAssignableFrom<DomainException>(new HeroUnavailableException(Guid.NewGuid()));
        Assert.IsAssignableFrom<DomainException>(new IncidentClosedException(Guid.NewGuid()));
        Assert.IsAssignableFrom<DomainException>(new AssignmentFullException(Guid.NewGuid()));
        Assert.IsAssignableFrom<DomainException>(new DuplicateAssignmentException(Guid.NewGuid(), Guid.NewGuid()));
        Assert.IsAssignableFrom<DomainException>(new InvalidIncidentStateException("Message"));
    }

    [Fact]
    public void EntityNotFoundException_IncludesEntityNameAndId()
    {
        var id = Guid.NewGuid();

        var exception = new EntityNotFoundException("Hero", id);

        Assert.Contains("Hero", exception.Message, StringComparison.Ordinal);
        Assert.Contains(id.ToString(), exception.Message, StringComparison.Ordinal);
    }
}
