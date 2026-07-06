using Microsoft.EntityFrameworkCore;

namespace Api;

public class ResetPasswordHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly TaskPlanDbContext _db;

    public ResetPasswordHandler(TaskPlanDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(p => p.Token == request.Token, cancellationToken);

        if (resetToken == null || !resetToken.IsValid)
            return Result.Failure(Error.Unauthorized("Auth.InvalidToken", "Invalid or expired password reset token."));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken);
        if (user == null)
            return Result.Failure(Error.NotFound("User.NotFound", "User associated with this token not found."));

        var newPasswordHash = PasswordService.HashPassword(request.NewPassword);
        user.ChangePassword(newPasswordHash);
        resetToken.MarkAsUsed();

        // Security: Revoke all sessions after password change
        var userSessions = await _db.Sessions
            .Where(s => s.UserId == user.Id && !s.RevokedAt.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var session in userSessions)
        {
            session.Revoke(DateTimeOffset.UtcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
