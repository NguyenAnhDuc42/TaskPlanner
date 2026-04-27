using System.Linq;
using Domain.Common;

namespace Domain.Entities;

public static class UserExtensions
{
    public static IQueryable<User> ById(this IQueryable<User> query, Guid id) => 
        query.Where(user => user.Id == id);

    public static IQueryable<User> ByEmail(this IQueryable<User> query, string email) => 
        query.Where(user => user.Email == email);

    public static IQueryable<User> WhereNotDeleted(this IQueryable<User> query) => 
        query.Where(user => user.DeletedAt == null);
}
