using Microsoft.EntityFrameworkCore;

namespace Application;

public class UpdateProfileHandler : ICommandHandler<UpdateProfileCommand>
{
    private readonly TaskPlanDbContext _db;
    private readonly CurrentUserService _currentUserService;

    public UpdateProfileHandler(TaskPlanDbContext db, CurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user is null) 
            return UserError.NotFound;

        if (request.Name is not null)
        {
            user.UpdateName(request.Name.Trim());
        }

        if (request.Email is not null)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailExists = await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id != userId && u.Email.ToLower() == normalizedEmail, cancellationToken);

            if (emailExists)
                return UserError.DuplicateEmail;

            user.UpdateEmail(request.Email.Trim());
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}



