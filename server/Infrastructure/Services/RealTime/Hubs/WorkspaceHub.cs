using System;
using Application.Interfaces.Services;
using Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using server.Application.Interfaces;

namespace Infrastructure.Services.RealTime.Hubs;

public class WorkspaceHub : Hub
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUserService;


    public WorkspaceHub(IPermissionService permissionService, ICurrentUserService currentUserService)
    {
        _permissionService = permissionService;
        _currentUserService = currentUserService;
    }

    public async Task JoinWorkspace(Guid workspaceId)
    {
        var userid = _currentUserService.CurrentUserId();
        await _permissionService.EnsurePermissionAsync(userid, workspaceId, Permission.View_Workspace);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace_{workspaceId}");
        await Clients.Caller.SendAsync("WorkspaceJoined", workspaceId);

    }

    public async Task LeaveWorkspace(Guid workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace_{workspaceId}");
    }

}
