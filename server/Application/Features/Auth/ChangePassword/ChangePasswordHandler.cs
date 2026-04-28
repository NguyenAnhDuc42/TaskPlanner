using Application.Common.Errors;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Interfaces;
using Application.Features;
using Domain.Entities;

namespace Application.Features.Auth;

public class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordService _passwordService;

    public ChangePasswordHandler(IDataBase db, ICurrentUserService currentUserService, IPasswordService passwordService)
    {
        _db = db;
        _currentUserService = currentUserService;
        _passwordService = passwordService;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = _currentUserService.CurrentUserId();
        if (userId == Guid.Empty) 
            return UserError.NotFound;
        
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user is null) 
            return UserError.NotFound;

        // Verify current password
        if (user.PasswordHash != null && !_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return AuthError.InvalidCredentials;

        if (user.PasswordHash == null && !string.IsNullOrEmpty(user.ExternalId))
            if (!string.IsNullOrEmpty(request.CurrentPassword))
                 return Error.Failure("Auth.ExternalUser", "External users cannot change password this way.");

        var newHash = _passwordService.HashPassword(request.NewPassword);
        user.ChangePassword(newHash);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
