using System;

namespace Domain.Entities.Support.Widget;

public record WidgetLayout
{
    public int Col { get; init; }     // left-most column index
    public int Row { get; init; }     // top-most row index
    public int Width { get; init; }   // width in grid units (e.g., 2, 3)
    public int Height { get; init; }  // height in grid units (e.g., 2, 4)

    // Allowed canonical sizes (extendable)
    private static readonly HashSet<(int w, int h)> AllowedSizes = new()
    {
        (1,1), (2,1), (2,2), (2,4), (3,2), (3,4), (4,2)
    };

    // EF
    private WidgetLayout() { }

    public WidgetLayout(int col, int row, int width, int height)
    {
        if (col < 0) throw new ArgumentOutOfRangeException(nameof(col));
        if (row < 0) throw new ArgumentOutOfRangeException(nameof(row));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        // enforce allowed canonical sizes; if you want arbitrary sizes remove this check
        if (!AllowedSizes.Contains((width, height)))
            throw new ArgumentException($"Unsupported widget size {width}x{height}. Allowed: {string.Join(", ", AllowedSizes.Select(s => $"{s.w}x{s.h}"))}");

        Col = col;
        Row = row;
        Width = width;
        Height = height;
    }
    public WidgetLayout WithPosition(int col, int row) => this with { Col = col, Row = row };
    public WidgetLayout WithSize(int width, int height) => new WidgetLayout(Col, Row, width, height);
}
