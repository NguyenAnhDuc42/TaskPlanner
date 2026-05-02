using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums.Workspace;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.UserFeatures;

public class GetUserPreferenceHandler(IDataBase db, ICurrentUserService currentUserService) : IQueryHandler<GetUserPreferenceQuery, UserPreferenceDto>
{
    public async Task<Result<UserPreferenceDto>> Handle(GetUserPreferenceQuery request, CancellationToken ct)
    {
        var user = await currentUserService.CurrentUserAsync(ct);
        var userId = user.Id;
        
        var preference = await db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (preference == null)
        {
            // Create default preferences if none exist
            var defaultSettings = new UserSetting 
            { 
                Theme = Theme.Dark, 
                SidebarWidth = 280, 
                SidebarCollapsed = false 
            };
            
            preference = UserPreference.Create(userId, defaultSettings);
            await db.UserPreferences.AddAsync(preference, ct);
            await db.SaveChangesAsync(ct);
        }

        return Result<UserPreferenceDto>.Success(new UserPreferenceDto(
            preference.UserId,
            preference.Settings.Theme,
            preference.Settings.LastWorkspaceId,
            preference.Settings.SidebarWidth,
            preference.Settings.SidebarCollapsed,
            preference.Settings.LayoutData,
            preference.Settings.WorkspaceSettings
        ));
    }
}
