using Idevs.Common;
using Idevs.Services;
using Idevs.Services.Implementations;
using Shouldly;

namespace Idevs.Tests.Services;

public sealed class ServiceImplementationTests
{
    [Fact]
    public void DefaultCurrentUser_ReturnsAnonymousUser()
    {
        // Arrange
        var anonymousId = Guid.NewGuid();
        var currentUser = new DefaultCurrentUser<Guid>(anonymousId);

        // Assert
        currentUser.Id.ShouldBe(anonymousId);
        currentUser.Name.ShouldBeNull();
        currentUser.IsAuthenticated.ShouldBeFalse();
        currentUser.Roles.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultCurrentUser_GetClaim_ReturnsNull()
    {
        // Arrange
        var currentUser = new DefaultCurrentUser<Guid>(Guid.Empty);

        // Act & Assert
        currentUser.GetClaim("any-claim").ShouldBeNull();
    }

    [Fact]
    public void DefaultCurrentUser_IsInRole_ReturnsFalse()
    {
        // Arrange
        var currentUser = new DefaultCurrentUser<Guid>(Guid.Empty);

        // Act & Assert
        currentUser.IsInRole("any-role").ShouldBeFalse();
    }

    [Fact]
    public void DefaultTenantContext_ReturnsDefaultTenant()
    {
        // Arrange
        var defaultId = Guid.NewGuid();
        var tenantContext = new DefaultTenantContext<Guid>(defaultId);

        // Assert
        tenantContext.TenantId.ShouldBe(defaultId);
        tenantContext.IsMultiTenant.ShouldBeFalse();
    }

    [Fact]
    public void DefaultTenantContext_TryGetTenantId_ReturnsTrue()
    {
        // Arrange
        var defaultId = Guid.NewGuid();
        var tenantContext = new DefaultTenantContext<Guid>(defaultId);

        // Act
        var success = tenantContext.TryGetTenantId(out var tenantId);

        // Assert
        success.ShouldBeTrue();
        tenantId.ShouldBe(defaultId);
    }

    [Fact]
    public void NoOpMetrics_IncrementCounter_DoesNotThrow()
    {
        // Arrange
        var metrics = new NoOpMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.IncrementCounter("test-counter", 1));
    }

    [Fact]
    public void NoOpMetrics_RecordGauge_DoesNotThrow()
    {
        // Arrange
        var metrics = new NoOpMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.RecordGauge("test-gauge", 42.5));
    }

    [Fact]
    public void NoOpMetrics_ObserveHistogram_DoesNotThrow()
    {
        // Arrange
        var metrics = new NoOpMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.ObserveHistogram("test-histogram", 100));
    }

    [Fact]
    public void NoOpMetrics_StartTimer_DoesNotThrow()
    {
        // Arrange
        var metrics = new NoOpMetrics();

        // Act & Assert
        Should.NotThrow(() =>
        {
            using var timer = metrics.StartTimer("test-timer");
        });
    }

    [Fact]
    public void Error_NotFound_CreatesNotFoundError()
    {
        // Arrange & Act
        var error = Error.NotFound("RES_404", "Resource not found");

        // Assert
        error.Type.ShouldBe(ErrorType.NotFound);
        error.Message.ShouldBe("Resource not found");
        error.Code.ShouldBe("RES_404");
        error.Details.ShouldBeNull();
    }

    [Fact]
    public void Error_Validation_CreatesValidationError()
    {
        // Arrange & Act
        var error = Error.Validation("VAL_001", "Validation failed");

        // Assert
        error.Type.ShouldBe(ErrorType.Validation);
        error.Message.ShouldBe("Validation failed");
        error.Code.ShouldBe("VAL_001");
        error.Details.ShouldBeNull();
    }

    [Fact]
    public void Error_Validation_WithDetails_CreatesValidationErrorWithDetails()
    {
        // Arrange & Act
        var details = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Name"] = new List<string> { "Name is required" }
        };
        var error = Error.Validation("VAL_002", "Validation failed", details);

        // Assert
        error.Type.ShouldBe(ErrorType.Validation);
        error.Message.ShouldBe("Validation failed");
        error.Code.ShouldBe("VAL_002");
        error.Details.ShouldNotBeNull();
        error.Details.ContainsKey("Name").ShouldBeTrue();
    }

    [Fact]
    public void Error_Conflict_CreatesConflictError()
    {
        // Arrange & Act
        var error = Error.Conflict("CONFLICT_001", "Resource already exists");

        // Assert
        error.Type.ShouldBe(ErrorType.Conflict);
        error.Message.ShouldBe("Resource already exists");
        error.Code.ShouldBe("CONFLICT_001");
        error.Details.ShouldBeNull();
    }

    [Fact]
    public void Error_Unauthorized_CreatesUnauthorizedError()
    {
        // Arrange & Act
        var error = Error.Unauthorized("AUTH_401", "Access denied");

        // Assert
        error.Type.ShouldBe(ErrorType.Unauthorized);
        error.Message.ShouldBe("Access denied");
        error.Code.ShouldBe("AUTH_401");
        error.Details.ShouldBeNull();
    }

    [Fact]
    public void Error_Forbidden_CreatesForbiddenError()
    {
        // Arrange & Act
        var error = Error.Forbidden("AUTH_403", "Insufficient permissions");

        // Assert
        error.Type.ShouldBe(ErrorType.Forbidden);
        error.Message.ShouldBe("Insufficient permissions");
        error.Code.ShouldBe("AUTH_403");
        error.Details.ShouldBeNull();
    }

    [Fact]
    public void Error_Failure_CreatesFailureError()
    {
        // Arrange & Act
        var error = Error.Failure("OP_FAILED", "Operation failed");

        // Assert
        error.Type.ShouldBe(ErrorType.Failure);
        error.Message.ShouldBe("Operation failed");
        error.Code.ShouldBe("OP_FAILED");
        error.Details.ShouldBeNull();
    }

    [Fact]
    public void Error_Unexpected_CreatesUnexpectedError()
    {
        // Arrange & Act
        var error = Error.Unexpected("UNEXP_500", "Unexpected error occurred");

        // Assert
        error.Type.ShouldBe(ErrorType.Unexpected);
        error.Message.ShouldBe("Unexpected error occurred");
        error.Code.ShouldBe("UNEXP_500");
        error.Details.ShouldBeNull();
    }
}
