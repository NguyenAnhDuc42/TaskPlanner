using Application.Common.Exceptions;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.Auth.VerifyEmail;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmailHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Set<User>()
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token, cancellationToken);

        if (user == null)
        {
            throw new InvalidTokenException("Invalid email verification token.");
        }

        try
        {
            user.VerifyEmail(request.Token);
            _unitOfWork.Set<User>().Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidTokenException(ex.Message);
        }
    }
}
