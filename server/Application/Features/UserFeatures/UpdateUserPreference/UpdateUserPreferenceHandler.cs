using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.UserFeatures;

public class UpdateUserPreferenceHandler(IDataBase db, ICurrentUserService currentUserService) : ICommandHandler<UpdateUserPreferenceCommand>
{
    public async Task<Result> Handle(UpdateUserPreferenceCommand request, CancellationToken ct)
    {
        var user = await currentUserService.CurrentUserAsync(ct);
        var userId = user.Id;

        var preference = await db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (preference == null)
        {
            // This should rarely happen if GetUserPreference is called first, but we handle it
            return Result.Failure(Application.Common.Errors.Error.Failure("UserPreference.NotFound", "User preference not found."));
        }

        // Mutate the JSONB POCO
        if (request.Theme.HasValue) preference.Setting.Theme = request.Theme.Value;
        if (request.ClearLastWorkspaceId) 
            preference.Setting.LastWorkspaceId = null;
        else if (request.LastWorkspaceId.HasValue) 
            preference.Setting.LastWorkspaceId = request.LastWorkspaceId.Value;
        if (request.SidebarWidth.HasValue) preference.Setting.SidebarWidth = request.SidebarWidth.Value;
        if (request.SidebarCollapsed.HasValue) preference.Setting.SidebarCollapsed = request.SidebarCollapsed.Value;
        if (request.LayoutData != null) preference.Setting.LayoutData = request.LayoutData;

        if (request.WorkspaceSettings != null)
        {
            foreach (var kvp in request.WorkspaceSettings)
            {
                preference.Setting.WorkspaceSettings[kvp.Key] = kvp.Value;
            }
        }

        // Inform EF Core that the entity has changed (since it's a JSONB converted column)
        db.UserPreferences.Update(preference);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
