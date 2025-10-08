namespace Idevs.Services.Implementations;

/// <summary>
/// Default single-tenant implementation of <see cref="ITenantContext{TTenantId}"/>.
/// Always returns a constant tenant ID and indicates single-tenant mode.
/// </summary>
/// <typeparam name="TTenantId">The type of the tenant identifier.</typeparam>
/// <param name="defaultTenantId">The default tenant identifier to use.</param>
public sealed class DefaultTenantContext<TTenantId>(TTenantId defaultTenantId) : ITenantContext<TTenantId>
    where TTenantId : notnull
{
    private readonly TTenantId _defaultTenantId = defaultTenantId ?? throw new ArgumentNullException(nameof(defaultTenantId));

    /// <inheritdoc />
    public TTenantId? TenantId => _defaultTenantId;

    /// <inheritdoc />
    public bool IsMultiTenant => false;

    /// <inheritdoc />
    public bool TryGetTenantId(out TTenantId? tenantId)
    {
        tenantId = _defaultTenantId;
        return true;
    }
}
