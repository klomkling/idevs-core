namespace Idevs.Services;

/// <summary>
/// Provides access to the current tenant context in a multi-tenant application.
/// </summary>
/// <typeparam name="TTenantId">The type of the tenant identifier (e.g., string, Guid, int).</typeparam>
public interface ITenantContext<TTenantId>
    where TTenantId : notnull
{
    /// <summary>
    /// Gets the current tenant identifier, or null if no tenant is resolved.
    /// </summary>
    TTenantId? TenantId { get; }

    /// <summary>
    /// Gets a value indicating whether the application is configured for multi-tenancy.
    /// </summary>
    bool IsMultiTenant { get; }

    /// <summary>
    /// Attempts to get the current tenant identifier.
    /// </summary>
    /// <param name="tenantId">When this method returns, contains the tenant identifier if available; otherwise, null.</param>
    /// <returns>true if a tenant identifier was resolved; otherwise, false.</returns>
    bool TryGetTenantId(out TTenantId? tenantId);
}
