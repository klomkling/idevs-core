using Idevs.Common;
using Shouldly;

namespace Idevs.Tests.Common;

public class ResultOfTTests
{
    [Fact]
    public void Success_WithValue_CreatesSuccessfulResult()
    {
        // Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Success_WithNullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        // Arrange
        var error = Error.NotFound("NOT_FOUND", "Item not found");

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Value_OnFailedResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var error = Error.Failure("FAIL", "Operation failed");
        var result = Result<int>.Failure(error);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Deconstruct_ReturnsAllComponents()
    {
        // Arrange
        var result = Result<string>.Success("test value");

        // Act
        var (isSuccess, value, error) = result;

        // Assert
        isSuccess.ShouldBeTrue();
        value.ShouldBe("test value");
        error.ShouldBeNull();
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        // Act
        Result<int> result = 42;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailure()
    {
        // Arrange
        var error = Error.Conflict("CONFLICT", "Conflict occurred");

        // Act
        Result<int> result = error;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }
}
