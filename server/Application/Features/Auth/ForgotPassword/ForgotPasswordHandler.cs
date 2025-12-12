using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<ForgotPasswordHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Set<User>().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        
        user.InitiatePasswordReset(token, TimeSpan.FromHours(1));
        
        _unitOfWork.Set<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var subject = "Reset your password";
        var body = $"You requested a password reset. Use this token: {token}";
        
        await _emailService.SendEmailAsync(user.Email, subject, body);
        
        _logger.LogInformation("Password reset token sent to {Email}", user.Email);
    }
}
