using Domain.Common;

namespace Domain.Entities;

public class UserPreference : Entity
{
    public Guid UserId { get; private set; }
    public UserSetting Setting { get; private set; } = new();

    private UserPreference() {}

    private UserPreference(Guid userId, UserSetting setting)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        Setting = setting;
    }

    public static UserPreference Create(Guid userId, UserSetting setting)
    {
        return new UserPreference(userId, setting);
    }

    public void UpdateSetting(UserSetting setting)
    {
        Setting = setting ?? throw new ArgumentNullException(nameof(setting));
    }
}