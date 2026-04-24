using System;
using System.Linq;

namespace Domain.Entities;

public static class SessionExtensions
{
    public static IQueryable<Session> WhereActive(this IQueryable<Session> query) => 
        query.Where(session => !session.RevokedAt.HasValue && session.ExpiresAt > DateTimeOffset.UtcNow);

    public static IQueryable<Session> ByUser(this IQueryable<Session> query, Guid userId) => 
        query.Where(session => session.UserId == userId);

    public static IQueryable<Session> ByRefreshToken(this IQueryable<Session> query, string token) => 
        query.Where(session => session.RefreshToken == token);
}
