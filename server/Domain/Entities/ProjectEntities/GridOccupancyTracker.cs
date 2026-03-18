namespace Domain.Entities.ProjectEntities;

public sealed class GridOccupancyTracker
{
    private readonly int _maxGridCols;
    private readonly int _maxGridRows;
    private readonly bool[,] _cells;
    private int _maxOccupiedRow = -1;

    public GridOccupancyTracker(int maxGridCols = 12, int maxGridRows = 2000)
    {
        _maxGridCols = maxGridCols;
        _maxGridRows = maxGridRows;
        _cells = new bool[maxGridRows, maxGridCols];
    }

    public void MarkOccupied(int col, int row, int width, int height)
    {
        for (int r = row; r < row + height; r++)
            for (int c = col; c < col + width; c++)
                _cells[r, c] = true;

        // Only grow — never shrink on mark
        int bottomRow = row + height - 1;
        if (bottomRow > _maxOccupiedRow)
            _maxOccupiedRow = bottomRow;
    }

    public void UnmarkOccupied(int col, int row, int width, int height)
    {
        for (int r = row; r < row + height; r++)
            for (int c = col; c < col + width; c++)
                _cells[r, c] = false;

        // Intentionally skip recomputing _maxOccupiedRow.
        // Slight over-scan on FindNextAvailablePosition is O(empty_rows × cols)
        // which is far cheaper than O(all_occupied_cells) on every remove.
    }

    public bool CanPlaceAt(int col, int row, int width, int height)
    {
        if (col < 0 || col + width > _maxGridCols ||
            row < 0 || row + height > _maxGridRows)
            return false;

        for (int r = row; r < row + height; r++)
            for (int c = col; c < col + width; c++)
                if (_cells[r, c]) return false;

        return true;
    }

    // Exclusive upper bound — safe to use directly in for (row < GetScanUpperBound(...))
    public int GetScanUpperBound(int widgetHeight) =>
        _maxOccupiedRow + widgetHeight + 1;
}