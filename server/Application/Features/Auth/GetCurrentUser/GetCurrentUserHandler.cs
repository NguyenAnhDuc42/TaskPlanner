using Application.Common.Results;
using Application.Features;
using server.Application.Interfaces;

namespace Application.Features.Auth.GetCurrentUser;

public class GetCurrentUserHandler : IQueryHandler<GetCurrentUserQuery, GetCurrentUserDto>
{
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<Result<GetCurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var user = await _currentUserService.CurrentUserAsync(ct);
        
        return new GetCurrentUserDto(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email
        );
    }
}
