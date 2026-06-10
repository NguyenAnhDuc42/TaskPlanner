namespace Application;

public class GetCurrentUserHandler : IQueryHandler<GetCurrentUserQuery, GetCurrentUserDto>
{
    private readonly CurrentUserService _currentUserService;

    public GetCurrentUserHandler(CurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<Result<GetCurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _currentUserService.CurrentUserAsync(cancellationToken);
        
        return Result<GetCurrentUserDto>.Success(new GetCurrentUserDto(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email
        ));
    }
}



