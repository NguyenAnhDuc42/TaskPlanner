using System;

namespace Domain.Enums;

[Flags]
public enum SpaceFeatures
{
    TimeTracking ,
    Priorities ,
    CustomFields ,
    All = TimeTracking | Priorities | CustomFields
}
