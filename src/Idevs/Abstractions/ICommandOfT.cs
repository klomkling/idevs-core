namespace Idevs.Abstractions;

/// <summary>
/// Marker interface for commands that return a value of type <typeparamref name="TResponse"/>.
/// Commands represent intentions to perform an action that changes system state.
/// </summary>
/// <typeparam name="TResponse">The type of value returned by the command.</typeparam>
public interface ICommand<out TResponse>
{
}
