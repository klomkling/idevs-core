using Idevs.Common;
using Shouldly;

namespace Idevs.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        // Arrange
        var error = Error.Validation("TEST001", "Test error");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Failure_WithNullError_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void Combine_WithAllSuccess_ReturnsSuccess()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();
        var result3 = Result.Success();

        // Act
        var combined = Result.Combine(result1, result2, result3);

        // Assert
        combined.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Combine_WithOneFailure_ReturnsFirstFailure()
    {
        // Arrange
        var result1 = Result.Success();
        var error2 = Error.Validation("ERR1", "First error");
        var result2 = Result.Failure(error2);
        var error3 = Error.Validation("ERR2", "Second error");
        var result3 = Result.Failure(error3);

        // Act
        var combined = Result.Combine(result1, result2, result3);

        // Assert
        combined.IsFailure.ShouldBeTrue();
        combined.Error.ShouldBe(error2);
    }

    [Fact]
    public void Deconstruct_ReturnsSuccessStateAndError()
    {
        // Arrange
        var error = Error.NotFound("NOT_FOUND", "Resource not found");
        var result = Result.Failure(error);

        // Act
        var (isSuccess, resultError) = result;

        // Assert
        isSuccess.ShouldBeFalse();
        resultError.ShouldBe(error);
    }
}
