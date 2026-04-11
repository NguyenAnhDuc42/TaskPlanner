using Application.Common.Results;
using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.Auth.ResetPassword;

public class ResetPasswordHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IDataBase _db;
    private readonly IPasswordService _passwordService;

    public ResetPasswordHandler(IDataBase db, IPasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var resetToken = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(p => p.Token == request.Token, ct);

        if (resetToken == null || !resetToken.IsValid)
            return Error.Unauthorized("Auth.InvalidToken", "Invalid or expired password reset token.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == resetToken.UserId, ct);
        if (user == null)
            return Error.NotFound("User.NotFound", "User associated with this token not found.");

        var newPasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.ChangePassword(newPasswordHash);
        resetToken.MarkAsUsed();
        
        // Security: Revoke all sessions after password change
        var userSessions = await _db.Sessions
            .Where(s => s.UserId == user.Id && !s.RevokedAt.HasValue)
            .ToListAsync(ct);

        foreach (var session in userSessions)
        {
            session.Revoke(DateTimeOffset.UtcNow);
        }
        
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
