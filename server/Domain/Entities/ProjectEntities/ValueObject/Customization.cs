using System;
using Domain.Common;

namespace Domain.Entities;

public readonly record struct Customization
{
    public string Color { get; init; }
    public string Icon { get; init; } = null!;

    public Customization(string color, string icon)
    {
        Color = color;
        Icon = icon;
    }

    public static Customization Create(string? color, string? icon)
    {
        var c = string.IsNullOrWhiteSpace(color) ? "#cdcbcbff" : color;
        var i = string.IsNullOrWhiteSpace(icon) ? "default_icon" : icon.Trim();
        return new Customization(c, i);
    }

    public static Customization CreateDefault()
        => new Customization("#cdcbcbff", "default_icon");

}
