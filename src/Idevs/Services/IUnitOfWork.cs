using Idevs.Common;

namespace Idevs.Services;

/// <summary>
/// Represents a unit of work pattern for managing database transactions.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Begins a new database transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes made in the unit of work to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the save operation.</returns>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}
