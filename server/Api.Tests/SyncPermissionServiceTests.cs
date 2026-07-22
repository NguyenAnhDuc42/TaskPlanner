using Domain;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Api.Tests;

public class SyncPermissionServiceTests
{
    private static SyncPermissionService CreateFor(Role role)
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
        return new SyncPermissionService(context);
    }

    [Theory]
    [InlineData(Role.Member)]
    [InlineData(Role.Admin)]
    [InlineData(Role.Owner)]
    public void RequireMember_Allows_MemberOrAbove(Role role)
    {
        var sut = CreateFor(role);
        sut.RequireMember(); // should not throw
    }

    [Fact]
    public void RequireMember_Throws_ForGuest()
    {
        var sut = CreateFor(Role.Guest);
        Assert.Throws<ForbiddenAccessException>(() => sut.RequireMember());
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Owner)]
    public void RequireAdmin_Allows_AdminOrAbove(Role role)
    {
        var sut = CreateFor(role);
        sut.RequireAdmin(); // should not throw
    }

    [Theory]
    [InlineData(Role.Guest)]
    [InlineData(Role.Member)]
    public void RequireAdmin_Throws_BelowAdmin(Role role)
    {
        var sut = CreateFor(role);
        Assert.Throws<ForbiddenAccessException>(() => sut.RequireAdmin());
    }

    [Fact]
    public void RequireCreatorOrAdmin_Allows_TheCreator_EvenAsGuest()
    {
        var member = WorkspaceMember.Create(
            userId: Guid.NewGuid(),
            workspaceId: Guid.NewGuid(),
            role: Role.Guest,
            status: MembershipStatus.Active,
            createdBy: Guid.NewGuid(),
            joinMethod: "Created");

        var context = new WorkspaceContext(new HttpContextAccessor());
        context.SetCurrentMember(member);
        var sut = new SyncPermissionService(context);

        // The creator of the resource IS the current member — should pass regardless of role.
        sut.RequireCreatorOrAdmin(member.Id);
    }

    [Fact]
    public void RequireCreatorOrAdmin_Allows_AdminActingOnSomeoneElsesResource()
    {
        var sut = CreateFor(Role.Admin);
        var someoneElsesId = Guid.NewGuid();
        sut.RequireCreatorOrAdmin(someoneElsesId); // should not throw
    }

    [Fact]
    public void RequireCreatorOrAdmin_Throws_ForNonCreatorNonAdmin()
    {
        var sut = CreateFor(Role.Member);
        var someoneElsesId = Guid.NewGuid();
        Assert.Throws<ForbiddenAccessException>(() => sut.RequireCreatorOrAdmin(someoneElsesId));
    }
}
