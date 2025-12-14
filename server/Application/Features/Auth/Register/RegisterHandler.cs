using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;
using Application.Common.Exceptions;

namespace Application.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(IUnitOfWork unitOfWork, IPasswordService passwordService, ILogger<RegisterHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user: {Email}", request.email);
        var exists = await _unitOfWork.Set<User>().AnyAsync(u => u.Email == request.email, cancellationToken);
        if (exists)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.email);
            throw new DuplicateEmailException(request.email);
        }

        var passwordHash = _passwordService.HashPassword(request.password);
        
        var user = User.Create(request.username, request.email, passwordHash);
        
        await _unitOfWork.Set<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered successfully: {UserId}, {Email}", user.Id, user.Email);

        return new RegisterResponse(user.Id, user.Name, user.Email);
    }
}
