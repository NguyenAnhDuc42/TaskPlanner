using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class ForgotPasswordHandler : ICommandHandler<ForgotPasswordCommand, string?>
{
    private readonly TaskPlanDbContext _db;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(TaskPlanDbContext db, ILogger<ForgotPasswordHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<string?>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return Result<string?>.Success(null);
        }

        var token = Guid.NewGuid().ToString("N");
        var resetToken = PasswordResetToken.Create(user.Id, token, TimeSpan.FromHours(1));
        
        await _db.PasswordResetTokens.AddAsync(resetToken, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset token generated for {Email}: {Token}", user.Email, token);
        return Result<string?>.Success(token);
    }
}


