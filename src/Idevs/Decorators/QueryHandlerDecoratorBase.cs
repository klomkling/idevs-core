using Idevs.Abstractions;
using Idevs.Common;

namespace Idevs.Decorators;

/// <summary>
/// Base class for query handler decorators.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the query.</typeparam>
public abstract class QueryHandlerDecoratorBase<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Gets the inner query handler being decorated.
    /// </summary>
    protected IQueryHandler<TQuery, TResponse> Inner { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryHandlerDecoratorBase{TQuery, TResponse}"/> class.
    /// </summary>
    /// <param name="inner">The inner handler to decorate.</param>
    protected QueryHandlerDecoratorBase(IQueryHandler<TQuery, TResponse> inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public abstract Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
