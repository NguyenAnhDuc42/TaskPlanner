using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, string?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(IUnitOfWork unitOfWork, ILogger<ForgotPasswordHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string?> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Set<User>().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return null;
        }

        var token = Guid.NewGuid().ToString("N");
        
        user.InitiatePasswordReset(token, TimeSpan.FromHours(1));
        
        _unitOfWork.Set<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Instead of sending email, return token directly (Dev/No-Email Mode)
        _logger.LogInformation("Password reset token generated for {Email}: {Token}", user.Email, token);
        return token;
    }
}
