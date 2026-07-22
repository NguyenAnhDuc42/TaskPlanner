using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests;

// Covers PermissionService.Verify — the pure, no-DB overload used by Update*/Delete* handlers
// that already loaded the entity. Space privacy + per-member access-level gating is the same
// logic that guards every private Space's Update/Delete path, so a mistake here is a real
// data-leak/permission bug, not a cosmetic one.
public class PermissionServiceTests
{
    private static (PermissionService sut, WorkspaceMember member) CreateFor(Role role)
    {
        var member = WorkspaceMember.Create(
            userId: Guid.NewGuid(),
            workspaceId: Guid.NewGuid(),
            role: role,
            status: MembershipStatus.Active,
            createdBy: Guid.NewGuid(),
            joinMethod: "Created");

        var context = new WorkspaceContext(new HttpContextAccessor());
        context.SetCurrentMember(member);

        var sut = new PermissionService(db: null!, context, NullLogger<PermissionService>.Instance);
        return (sut, member);
    }

    [Fact]
    public void Verify_Fails_WhenCallerRoleBelowRequiredRole()
    {
        var (sut, _) = CreateFor(Role.Guest);

        var allowed = sut.Verify(Role.Member, isPrivate: false, callerAccessLevel: null);

        Assert.False(allowed);
    }

    [Fact]
    public void Verify_Allows_AdminRegardlessOfPrivacyOrAccess()
    {
        var (sut, _) = CreateFor(Role.Admin);

        // Private space, no access record at all — Admin should still pass.
        var allowed = sut.Verify(Role.Member, isPrivate: true, callerAccessLevel: null, requiredAccess: AccessLevel.Manager);

        Assert.True(allowed);
    }

    [Fact]
    public void Verify_Allows_NonPrivateSpace_WithNoAccessRecord()
    {
        var (sut, _) = CreateFor(Role.Member);

        var allowed = sut.Verify(Role.Member, isPrivate: false, callerAccessLevel: null, requiredAccess: AccessLevel.Manager);

        Assert.True(allowed);
    }

    [Fact]
    public void Verify_Fails_PrivateSpace_WithNoAccessRecord()
    {
        var (sut, _) = CreateFor(Role.Member);

        var allowed = sut.Verify(Role.Member, isPrivate: true, callerAccessLevel: null, requiredAccess: AccessLevel.Viewer);

        Assert.False(allowed);
    }

    [Fact]
    public void Verify_Allows_TheCreator_EvenWithNoAccessRecord()
    {
        var (sut, member) = CreateFor(Role.Member);

        var allowed = sut.Verify(Role.Member, isPrivate: true, callerAccessLevel: null, requiredAccess: AccessLevel.Manager, creatorId: member.Id);

        Assert.True(allowed);
    }

    [Fact]
    public void Verify_Fails_PrivateSpace_WhenAccessLevelBelowRequired()
    {
        var (sut, _) = CreateFor(Role.Member);

        var allowed = sut.Verify(Role.Member, isPrivate: true, callerAccessLevel: AccessLevel.Viewer, requiredAccess: AccessLevel.Manager);

        Assert.False(allowed);
    }

    [Fact]
    public void Verify_Allows_PrivateSpace_WhenAccessLevelMeetsRequired()
    {
        var (sut, _) = CreateFor(Role.Member);

        var allowed = sut.Verify(Role.Member, isPrivate: true, callerAccessLevel: AccessLevel.Manager, requiredAccess: AccessLevel.Manager);

        Assert.True(allowed);
    }
}
