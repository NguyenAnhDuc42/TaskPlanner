using System;
using src.Domain.Entities.UserEntity;
using src.Domain.Valueobject;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IUserRepository
{
    Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userUd,CancellationToken cancellationToken = default); 
}
