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
        var user = await _unitOfWork.Set<User>()
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user == null)
        {
            throw new InvalidTokenException("Invalid or expired password reset token.");
        }

        var newPasswordHash = _passwordService.HashPassword(request.NewPassword);

        try 
        {
            user.CompletePasswordReset(request.Token, newPasswordHash);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidTokenException(ex.Message);
        }

        _unitOfWork.Set<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
