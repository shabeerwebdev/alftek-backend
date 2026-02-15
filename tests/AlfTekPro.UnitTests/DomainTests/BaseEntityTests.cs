using AlfTekPro.Domain.Common;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.UnitTests.DomainTests;

/// <summary>
/// Unit tests for BaseEntity class
/// </summary>
public class BaseEntityTests
{
    private class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void BaseEntity_ShouldGenerateId_WhenCreated()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void BaseEntity_ShouldSetCreatedAt_WhenCreated()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var entity = new TestEntity();
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        entity.CreatedAt.Should().BeAfter(beforeCreation);
        entity.CreatedAt.Should().BeBefore(afterCreation);
    }

    [Fact]
    public void BaseEntity_UpdatedAt_ShouldBeNull_Initially()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.UpdatedAt.Should().BeNull();
    }
}
