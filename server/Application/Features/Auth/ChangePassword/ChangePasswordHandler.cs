using Microsoft.EntityFrameworkCore;

namespace Application;

public class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly TaskPlanDbContext _db;
    private readonly CurrentUserService _currentUserService;

    public ChangePasswordHandler(TaskPlanDbContext db, CurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        if (userId == Guid.Empty) 
            return UserError.NotFound;
        
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) 
            return UserError.NotFound;

        if (user.PasswordHash != null && !PasswordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return AuthError.InvalidCredentials;

        if (user.PasswordHash == null && !string.IsNullOrEmpty(user.ExternalId))
            if (!string.IsNullOrEmpty(request.CurrentPassword))
                 return Error.Failure("Auth.ExternalUser", "External users cannot change password this way.");

        var newHash = PasswordService.HashPassword(request.NewPassword);
        user.ChangePassword(newHash);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}



