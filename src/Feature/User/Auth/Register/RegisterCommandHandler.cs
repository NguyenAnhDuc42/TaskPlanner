using System;
using MediatR;
using src.Domain.Entities.UserEntity;
using src.Domain.Valueobject;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;
using src.Infrastructure.Services;

namespace src.Feature.User.Auth.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IUserRepository _userRepository;
    public RegisterCommandHandler(PlannerDbContext context, IUserRepository userRepository, IPasswordService passwordService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
    }
    public async Task<Result<RegisterResponse, ErrorResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
 
            var email = new Email(request.email);
            if (await _userRepository.IsEmailExistsAsync(email, cancellationToken))
                return Result<RegisterResponse, ErrorResponse>.Failure(ErrorResponse.Conflict("Email already exists", $"Email {request.email} already exists. Please try another email."));
            var passwordhash = _passwordService.HashPassword(request.password);
            var user = Domain.Entities.UserEntity.User.Create(request.username, email, passwordhash);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return Result<RegisterResponse, ErrorResponse>.Success(new RegisterResponse(user.Email));

    }
}
