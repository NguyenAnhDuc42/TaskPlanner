using System;
using System.Security.Claims;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Infrastructure.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TaskPlanDbContext _dbContext;
    public CurrentUserService(IHttpContextAccessor httpContextAccessor, TaskPlanDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public User CurrentUser()
    {
        var user = _dbContext.Users.Find(CurrentUserId());
        if (user == null) throw new UnauthorizedAccessException("Unauthorized");
        return user!;
    }

    public Guid CurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        { return Guid.Empty; }
        return userId;
    }


}
