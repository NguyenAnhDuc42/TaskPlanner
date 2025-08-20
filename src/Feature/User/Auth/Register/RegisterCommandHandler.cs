using System;
using MediatR;
using src.Domain.Entities.UserEntity;
using src.Domain.Entities.WorkspaceEntity;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.User.Auth.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse, ErrorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public RegisterCommandHandler(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
    }

    public async Task<Result<RegisterResponse, ErrorResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Users.IsEmailExistsAsync(request.email, cancellationToken))
        {
            return Result<RegisterResponse, ErrorResponse>.Failure(ErrorResponse.Conflict("Email already exists", $"Email {request.email} already exists. Please try another email."));
        }

        var passwordhash = _passwordService.HashPassword(request.password);
        var user = Domain.Entities.UserEntity.User.Create(request.username, request.email, passwordhash);
        var workspace = Workspace.CreateSampleWorkspace(user.Id);
        user.CreateWorkspace(workspace);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegisterResponse, ErrorResponse>.Success(new RegisterResponse(user.Email));
    }
}