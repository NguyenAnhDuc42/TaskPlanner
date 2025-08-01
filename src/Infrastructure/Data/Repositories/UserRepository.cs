using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.UserEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class UserRepository : BaseRepository<User> ,IUserRepository
{
    public UserRepository(PlannerDbContext context) : base(context) { }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
         return await Context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetUserByIdAsync(Guid userUd, CancellationToken cancellationToken = default)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.Id == userUd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<User>> GetUsersByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
    {
        return await Context.Users
            .Where(u => emails.Contains(u.Email))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Users.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false);
    }
}
