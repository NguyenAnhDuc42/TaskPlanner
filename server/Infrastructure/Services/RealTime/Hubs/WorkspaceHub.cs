using System;
using Application.Interfaces.Services;
using Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using server.Application.Interfaces;

namespace Infrastructure.Services.RealTime.Hubs;

public class WorkspaceHub : Hub
{
    private readonly IWorkspacePermissionService _permissionService;
    private readonly ICurrentUserService _currentUserService;
    public WorkspaceHub(IWorkspacePermissionService permissionService, ICurrentUserService currentUserService)
    {
        _permissionService = permissionService;
        _currentUserService = currentUserService;
    }

    public async Task JoinWorkspace(Guid workspaceId)
    {
        var userid = _currentUserService.CurrentUserId();
        var member = await _permissionService.CheckForUser(workspaceId, userid);
        if (member == Role.None) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace_{workspaceId}");
        await Clients.Caller.SendAsync("WorkspaceJoined", workspaceId);

    }

    public async Task LeaveWorkspace(Guid workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace_{workspaceId}");
    }

}
