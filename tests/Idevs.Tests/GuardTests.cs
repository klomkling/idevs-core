using Xunit;
using Shouldly;

namespace Idevs.Tests;

public class GuardTests
{
    [Fact]
    public void NotNull_WithNonNullValue_ReturnsOriginalValue()
    {
        // Arrange
        var testObject = new object();

        // Act
        var result = Guard.NotNull(testObject, nameof(testObject));

        // Assert
        result.ShouldBeSameAs(testObject);
    }

    [Fact]
    public void NotNull_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        object? testObject = null;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            Guard.NotNull(testObject, nameof(testObject)));

        exception.ParamName.ShouldBe(nameof(testObject));
    }

    [Theory]
    [InlineData("valid string")]
    [InlineData("  text with spaces  ")]
    [InlineData("x")]
    public void NotNullOrWhiteSpace_WithValidString_ReturnsOriginalString(string value)
    {
        // Act
        var result = Guard.NotNullOrWhiteSpace(value, nameof(value));

        // Assert
        result.ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void NotNullOrWhiteSpace_WithInvalidString_ThrowsArgumentException(string? value)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            Guard.NotNullOrWhiteSpace(value, nameof(value)));

        exception.ParamName.ShouldBe(nameof(value));
        // .NET 8 ThrowIfNullOrWhiteSpace message
        exception.Message.ShouldContain("cannot be an empty string or composed entirely of whitespace");
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            Guard.NotNullOrWhiteSpace(value, nameof(value)));

        exception.ParamName.ShouldBe(nameof(value));
    }

    [Fact]
    public void NotNull_WithComplexType_ReturnsOriginalValue()
    {
        // Arrange
        var testData = new TestData { Id = 42, Name = "Test" };

        // Act
        var result = Guard.NotNull(testData, nameof(testData));

        // Assert
        result.ShouldBeSameAs(testData);
        result.Id.ShouldBe(42);
        result.Name.ShouldBe("Test");
    }

    [Fact]
    public void NotNull_WithNullComplexType_ThrowsArgumentNullException()
    {
        // Arrange
        TestData? testData = null;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            Guard.NotNull(testData, nameof(testData)));

        exception.ParamName.ShouldBe(nameof(testData));
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
