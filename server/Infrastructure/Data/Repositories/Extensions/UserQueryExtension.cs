using System;
using Domain.Entities;

namespace Infrastructure.Data.Repositories.Extensions;

public static class UserQueryExtension
{
    public static IQueryable<User> WhereUsername(this IQueryable<User> query, string username)
    {
        return query.Where(u => u.Name == username);
    }

    public static IQueryable<User> WhereEmail(this IQueryable<User> query, string email)
    {
        return query.Where(u => u.Email == email);
    }
}