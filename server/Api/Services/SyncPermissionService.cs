namespace Api;

public class SyncPermissionService(WorkspaceContext ctx)
{
    public void RequireMember()
    {
        if (!ctx.CurrentMember!.Role.IsAtLeast(Role.Member))
            throw new ForbiddenAccessException(MemberError.DontHavePermission.Code);
    }

    public void RequireAdmin()
    {
        if (!ctx.CurrentMember!.Role.IsAtLeast(Role.Admin))
            throw new ForbiddenAccessException(MemberError.DontHavePermission.Code);
    }

    public void RequireCreatorOrAdmin(Guid creatorId)
    {
        var member = ctx.CurrentMember!;
        if (member.Id != creatorId && !member.Role.IsAtLeast(Role.Admin))
            throw new ForbiddenAccessException(MemberError.DontHavePermission.Code);
    }
}
