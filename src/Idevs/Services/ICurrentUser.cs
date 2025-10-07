namespace Idevs.Services;

/// <summary>
/// Provides access to the current authenticated user's information.
/// </summary>
/// <typeparam name="TUserId">The type of the user identifier (e.g., string, Guid, int).</typeparam>
public interface ICurrentUser<TUserId>
    where TUserId : notnull
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// </summary>
    TUserId Id { get; }

    /// <summary>
    /// Gets the name of the current user, or null if not available.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the roles assigned to the current user.
    /// </summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Gets a specific claim value for the current user.
    /// </summary>
    /// <param name="claimType">The type of claim to retrieve.</param>
    /// <returns>The claim value if found; otherwise, null.</returns>
    string? GetClaim(string claimType);

    /// <summary>
    /// Determines whether the current user has the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>true if the user has the role; otherwise, false.</returns>
    bool IsInRole(string role);
}
