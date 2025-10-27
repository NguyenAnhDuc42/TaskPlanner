using Application.Common;

namespace Infrastructure.Services.Permissions;

public record class PermissionRule
{
    public required Func<PermissionContext, bool> Evaluate { get; init; }
    public required string Description { get; init; }
}
