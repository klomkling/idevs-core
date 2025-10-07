namespace Idevs.Abstractions;

/// <summary>
/// Marker interface for queries that return a value of type <typeparamref name="TResponse"/>.
/// Queries represent read-only operations that retrieve data without changing system state.
/// </summary>
/// <typeparam name="TResponse">The type of value returned by the query.</typeparam>
public interface IQuery<out TResponse>
{
}
