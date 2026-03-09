using Application.Common.Exceptions;
using Application.Contract.UserContract;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.Auth.UpdateProfile;

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProfileHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        var user = await _unitOfWork.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Current user not found.");

        if (request.Name is not null)
        {
            user.UpdateName(request.Name.Trim());
        }

        if (request.Email is not null)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailExists = await _unitOfWork.Set<User>()
                .AsNoTracking()
                .AnyAsync(u => u.Id != userId && u.Email.ToLower() == normalizedEmail, cancellationToken);

            if (emailExists)
            {
                throw new DuplicateEmailException(request.Email);
            }

            user.UpdateEmail(request.Email.Trim());
        }


        return new UserDto(user.Id, user.Name, user.Email);
    }
}

