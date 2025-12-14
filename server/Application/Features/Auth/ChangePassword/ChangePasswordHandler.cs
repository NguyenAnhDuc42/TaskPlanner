using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.Auth.ChangePassword;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordService _passwordService;

    public ChangePasswordHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _passwordService = passwordService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        // Interface returns Guid, so no null check needed per signature, but good to be safe if implementation differs.
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();
        
        var user = await _unitOfWork.Set<User>().FindAsync(new object[] { userId }, cancellationToken);

        if (user == null) throw new UnauthorizedAccessException();

        // Verify current password
        if (user.PasswordHash != null && !_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
             throw new UnauthorizedAccessException("Invalid current password.");
        }
        else if (user.PasswordHash == null && !string.IsNullOrEmpty(user.ExternalId))
        {
            // Edge case: OAuth user trying to set password? Allow it, but they don't have a current password to verify.
            // Requirement says "CurrentPassword" is mandatory. For OAuth users setting password for the first time, 
            // we might need a different flow (SetPassword).
            // For now, assuming standard flow for password users.
            if (!string.IsNullOrEmpty(request.CurrentPassword))
            {
                 throw new UnauthorizedAccessException("External users cannot change password like this.");
            }
        }

        var newHash = _passwordService.HashPassword(request.NewPassword);
        user.ChangePassword(newHash);

        _unitOfWork.Set<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
