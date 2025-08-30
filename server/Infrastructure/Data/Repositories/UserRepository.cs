using System;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Infrastructure.Data.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{   
    public UserRepository(TaskPlanDbContext context) : base(context) { }

    public async Task<User?> GetUsersByEmail(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}