using System;
using src.Domain.Entities.UserEntity;
using src.Domain.Valueobject;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IUserRepository
{
    Task<bool> IsEmailExistsAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userUd,CancellationToken cancellationToken = default); 
}
