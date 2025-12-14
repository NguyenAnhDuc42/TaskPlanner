using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using server.Application.Interfaces;
using Application.Common.Exceptions;

namespace Application.Features.Auth.ResetPassword;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public ResetPasswordHandler(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await _unitOfWork.Set<PasswordResetToken>()
            .FirstOrDefaultAsync(p => p.Token == request.Token, cancellationToken);

        if (resetToken == null || !resetToken.IsValid)
        {
            throw new InvalidTokenException("Invalid or expired password reset token.");
        }

        var user = await _unitOfWork.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidTokenException("User not found.");
        }

        var newPasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.ChangePassword(newPasswordHash);
        resetToken.MarkAsUsed();

        _unitOfWork.Set<User>().Update(user);
        _unitOfWork.Set<PasswordResetToken>().Update(resetToken);
        
        // Security: Revoke all sessions after password change
        user.LogoutAllSessions();
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
