namespace Application;

public record GetFavoritesQuery() : IQueryRequest<List<FavoriteRecord>>;
