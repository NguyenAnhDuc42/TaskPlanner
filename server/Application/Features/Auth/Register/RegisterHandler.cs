using System;
using Application.Contract.UserContract;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;

    public RegisterHandler(IUnitOfWork unitOfWork, IPasswordService passwordService, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }
    public async Task<string> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existedEmail = await _unitOfWork.Set<User>().AnyAsync(u => u.Email == request.email, cancellationToken);
        if (existedEmail)
        {
            throw new Exception("Email already exists");
        }
        var passwordhash = _passwordService.HashPassword(request.password);
        var user = User.Create(request.username, request.email, passwordhash);
        await _unitOfWork.Set<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var message = $"User {user.Email} registered successfully";
        return message;
    }
}
