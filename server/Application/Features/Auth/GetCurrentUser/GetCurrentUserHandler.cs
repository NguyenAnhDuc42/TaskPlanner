using MediatR;
using server.Application.Interfaces;
using Application.Contract.UserContract;

namespace Application.Features.Auth.GetCurrentUser;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = _currentUserService.CurrentUser();
        
        return new UserDto(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email
        );
    }
}
