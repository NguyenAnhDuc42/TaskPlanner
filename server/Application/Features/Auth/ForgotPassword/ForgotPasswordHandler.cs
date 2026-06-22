using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application;

public class ForgotPasswordHandler(
    TaskPlanDbContext db,
    SmtpEmailService emailService,
    IOptions<AppSettings> appOptions,
    ILogger<ForgotPasswordHandler> logger
) : ICommandHandler<ForgotPasswordCommand, string?>
{
    public async Task<Result<string?>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return Result<string?>.Success(null);
        }

        var token = Guid.NewGuid().ToString("N");
        var resetToken = PasswordResetToken.Create(user.Id, token, TimeSpan.FromHours(1));

        await db.PasswordResetTokens.AddAsync(resetToken, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var frontendUrl = appOptions.Value.FrontendUrl;
        var resetLink = $"{frontendUrl}/auth/reset-password?token={token}";
        var html = BuildResetEmail(user.Name, resetLink);

        await emailService.SendAsync(user.Email, "Reset your TaskPlanner password", html, cancellationToken);

        return Result<string?>.Success(null);
    }

    private static string BuildResetEmail(string name, string resetLink) => $"""
        <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:32px">
          <h2 style="margin:0 0 8px">Reset your password</h2>
          <p style="color:#555;margin:0 0 24px">Hi {name}, click the button below to choose a new password. This link expires in 1 hour.</p>
          <a href="{resetLink}"
             style="display:inline-block;padding:12px 24px;background:#6366f1;color:#fff;border-radius:8px;text-decoration:none;font-weight:600">
            Reset password
          </a>
          <p style="color:#999;font-size:12px;margin:24px 0 0">If you didn't request this, you can safely ignore this email.</p>
        </div>
        """;
}
