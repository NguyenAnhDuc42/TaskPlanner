namespace Application;

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQueryRequest<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}


