using System;

namespace Domain.Entities.Support.Widget;

public record WidgetLayout
{
    public int Col { get; init; }
    public int Row { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    private static readonly HashSet<(int w, int h)> AllowedSizes = new()
        {
            (1,1), (2,1), (2,2), (2,4), (3,2), (3,4), (4,2)
        };

    private WidgetLayout() { } // EF

    public WidgetLayout(int col, int row, int width, int height)
    {
        if (col < 0) throw new ArgumentOutOfRangeException(nameof(col));
        if (row < 0) throw new ArgumentOutOfRangeException(nameof(row));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (!AllowedSizes.Contains((width, height)))
            throw new ArgumentException($"Unsupported widget size {width}x{height}.");

        Col = col; Row = row; Width = width; Height = height;
    }

    public WidgetLayout WithPosition(int col, int row) => this with { Col = col, Row = row };
    public WidgetLayout WithSize(int width, int height) => new WidgetLayout(Col, Row, width, height);
}