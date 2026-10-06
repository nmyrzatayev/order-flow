using FluentAssertions;
using NetArchTest.Rules;
using OrderFlow.Domain.Common;

namespace OrderFlow.ArchitectureTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "OrderFlow.Domain";
    private const string ApplicationNamespace = "OrderFlow.Application";
    private const string InfrastructureNamespace = "OrderFlow.Infrastructure";
    private const string ApiNamespace = "OrderFlow.Api";

    [Fact]
    public void Domain_ShouldNotHaveDependencyOnOtherProjects()
    {
        // Arrange
        var assembly = typeof(IDomainEvent).Assembly;

        var otherProjects = new[]
        {
            ApplicationNamespace,
            InfrastructureNamespace,
            ApiNamespace
        };

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DomainEvents_ShouldBeSealedAndImplementIDomainEvent()
    {
        // Arrange
        var assembly = typeof(IDomainEvent).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(DomainEvent))
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
}
