namespace Domain.Entities.ProjectEntities.ValueObject;

public record class Customization
{
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;

    public Customization() { }
    private Customization(string color, string icon)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Color must be provided.", nameof(color));

        if (!Common.ColorValidator.IsValidColorCode(color))
            throw new ArgumentException("Invalid color format.", nameof(color));

        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Icon must be provided.", nameof(icon));

        Color = color;
        Icon = icon;
    }

    public static Customization Create(string color, string icon)
       => new Customization(color.Trim(), icon.Trim());

    public static Customization CreateDefault()
        => new Customization("#cdcbcbff", "default_icon");

}

