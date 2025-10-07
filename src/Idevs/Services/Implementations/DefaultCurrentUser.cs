namespace Idevs.Services.Implementations;

/// <summary>
/// Default anonymous user implementation of <see cref="ICurrentUser{TUserId}"/>.
/// Represents an unauthenticated user with no roles or claims.
/// </summary>
/// <typeparam name="TUserId">The type of the user identifier.</typeparam>
/// <param name="anonymousUserId">The identifier to use for anonymous users.</param>
public sealed class DefaultCurrentUser<TUserId>(TUserId anonymousUserId) : ICurrentUser<TUserId>
    where TUserId : notnull
{
    private readonly TUserId _anonymousUserId = anonymousUserId ?? throw new ArgumentNullException(nameof(anonymousUserId));

    /// <inheritdoc />
    public TUserId Id => _anonymousUserId;

    /// <inheritdoc />
    public string? Name => null;

    /// <inheritdoc />
    public bool IsAuthenticated => false;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles => [];

    /// <inheritdoc />
    public string? GetClaim(string claimType) => null;

    /// <inheritdoc />
    public bool IsInRole(string role) => false;
}
