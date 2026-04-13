using Application.Common.Results;
using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler : ICommandHandler<ForgotPasswordCommand, string?>
{
    private readonly IDataBase _db;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(IDataBase db, ILogger<ForgotPasswordHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<string?>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return Result<string?>.Success(null);
        }

        var token = Guid.NewGuid().ToString("N");
        var resetToken = PasswordResetToken.Create(user.Id, token, TimeSpan.FromHours(1));
        
        await _db.PasswordResetTokens.AddAsync(resetToken, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Password reset token generated for {Email}: {Token}", user.Email, token);
        return Result<string?>.Success(token);
    }
}
