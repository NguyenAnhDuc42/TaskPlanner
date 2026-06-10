namespace Application;

public class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly TaskPlanDbContext _db;
    private readonly CurrentUserService _currentUserService;
    private readonly PasswordService _passwordService;

    public ChangePasswordHandler(TaskPlanDbContext db, CurrentUserService currentUserService, PasswordService passwordService)
    {
        _db = db;
        _currentUserService = currentUserService;
        _passwordService = passwordService;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        if (userId == Guid.Empty) 
            return UserError.NotFound;
        
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
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

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}



