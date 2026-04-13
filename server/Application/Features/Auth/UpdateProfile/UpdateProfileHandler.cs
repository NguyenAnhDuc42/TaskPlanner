using Application.Common.Errors;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Interfaces;
using Application.Features;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.UpdateProfile;

public class UpdateProfileHandler : ICommandHandler<UpdateProfileCommand, UpdateProfileDto>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProfileHandler(IDataBase db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UpdateProfileDto>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var userId = _currentUserService.CurrentUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        
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
                .AnyAsync(u => u.Id != userId && u.Email.ToLower() == normalizedEmail, ct);

            if (emailExists)
                return UserError.DuplicateEmail;

            user.UpdateEmail(request.Email.Trim());
        }

        await _db.SaveChangesAsync(ct);

        return Result<UpdateProfileDto>.Success(new UpdateProfileDto(user.Id, user.Name, user.Email));
    }
}
