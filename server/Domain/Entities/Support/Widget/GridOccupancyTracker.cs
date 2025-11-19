using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.Support.Widget;

public class GridOccupancyTracker
{
    private readonly int _maxGridCols;
    private readonly int _maxCanvasHeight;
    private readonly Dictionary<(int row, int col), bool> _occupiedCells = new();
    private int _maxOccupiedRow = -1;

    public GridOccupancyTracker(int maxGridCols = 12, int maxCanvasHeight = 2000)
    {
        _maxGridCols = maxGridCols;
        _maxCanvasHeight = maxCanvasHeight;
    }

    public void MarkOccupied(int col, int row, int width, int height)
    {
        for (int r = row; r < row + height; r++)
        {
            for (int c = col; c < col + width; c++)
            {
                _occupiedCells[(r, c)] = true;
            }
        }
        int bottomRow = row + height - 1;
        if (bottomRow > _maxOccupiedRow)
            _maxOccupiedRow = bottomRow;
    }

    public void UnmarkOccupied(int col, int row, int width, int height)
    {
        for (int r = row; r < row + height; r++)
        {
            for (int c = col; c < col + width; c++)
            {
                _occupiedCells.Remove((r, c));
            }
        }
        _maxOccupiedRow = _occupiedCells.Any() ? _occupiedCells.Keys.Max(k => k.row) : -1;
    }

    public bool CanPlaceAt(int col, int row, int width, int height)
    {
        if (col < 0 || col + width > _maxGridCols || row < 0 || row + height > _maxCanvasHeight)
            return false;

        for (int r = row; r < row + height; r++)
        {
            for (int c = col; c < col + width; c++)
            {
                if (_occupiedCells.TryGetValue((r, c), out var occupied) && occupied)
                    return false;
            }
        }
        return true;
    }

    public int GetMaxScanRow(int widgetHeight) => _maxOccupiedRow + widgetHeight;
}
