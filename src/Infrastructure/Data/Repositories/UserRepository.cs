using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.UserEntity;
using src.Domain.Valueobject;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PlannerDbContext _context;
    public UserRepository(PlannerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
         return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetUserByIdAsync(Guid userUd, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userUd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false);
    }
}
