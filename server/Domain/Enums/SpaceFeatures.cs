using System;

namespace Domain.Enums;

[Flags]
public enum SpaceFeatures
{
    TimeTracking = 1,
    Priorities = 2,
    CustomFields = 4,
    All = TimeTracking | Priorities | CustomFields
}
