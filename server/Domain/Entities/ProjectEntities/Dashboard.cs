using Domain.Common;
using Domain.Enums.RelationShip;
using Domain.Enums.Widget;

namespace Domain.Entities;

public class Dashboard : Entity
{
    public EntityLayerType LayerType { get; private set; }
    public Guid LayerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsShared { get; private set; }
    public bool IsMain { get; private set; }

    private readonly List<Widget> _widgets = new();
    public IReadOnlyCollection<Widget> Widgets => _widgets;

    private Dashboard() { }

    private Dashboard(Guid id, EntityLayerType layerType, Guid layerId, string name, bool isShared, Guid creatorId, bool isMain = false)
        : base(id)
    {
        LayerType = layerType;
        LayerId = layerId;
        Name = name;
        IsShared = isShared;
        IsMain = isMain;
        CreatorId = creatorId;
    }

    public static Dashboard CreateWorkspaceDashboard(Guid workspaceId, Guid creatorId, string name, bool isShared = false, bool isMain = false)
    {
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));

        return new(Guid.NewGuid(), EntityLayerType.ProjectWorkspace, workspaceId, name, isShared, creatorId, isMain);
    }

    public static Dashboard CreateScopedDashboard( EntityLayerType layerType, Guid layerId, Guid creatorId, string name, bool isShared = false, bool isMain = false)
    {
        if (layerType == EntityLayerType.ProjectWorkspace) throw new ArgumentException("Use CreateWorkspaceDashboard for workspace scope.", nameof(layerType));
        if (layerId == Guid.Empty) throw new ArgumentException("LayerId cannot be empty.", nameof(layerId));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));

        return new(Guid.NewGuid(), layerType, layerId, name, isShared, creatorId, isMain);
    }

    public void AddWidget(WidgetType widgetType, string configJson, int col, int row, int width, int height, Guid creatorId)
    {
        var layout = new WidgetLayout(col, row, width, height);
        var widget = new Widget(
            Guid.NewGuid(), Id, layout, LayerType, LayerId,
            widgetType, configJson, creatorId);

        _widgets.Add(widget);
        UpdateTimestamp();
    }

    public void RemoveWidget(Guid widgetId)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null) return;

        _widgets.Remove(widget);
        UpdateTimestamp();
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.");
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateShared(bool isShared) { IsShared = isShared; UpdateTimestamp(); }
    public void UpdateMain(bool isMain) { IsMain = isMain; UpdateTimestamp(); }

    /* ─── Commanded out: Over-engineered logic ───────────────────────
    private const int MaxGridCols = 12;
    private const int MaxGridRows = 2000;
    private const int MaxCascadeDepth = 1000;
    private const int MaxScanRows = 1000;

    private GridOccupancyTracker? _occupancyTracker;
    private GridOccupancyTracker OccupancyTracker
    {
        get
        {
            if (_occupancyTracker is not null) return _occupancyTracker;

            _occupancyTracker = new GridOccupancyTracker(MaxGridCols, MaxGridRows);
            foreach (var w in _widgets)
                _occupancyTracker.MarkOccupied(
                    w.Layout.Col, w.Layout.Row,
                    w.Layout.Width, w.Layout.Height);

            return _occupancyTracker;
        }
    }

    public void MoveWidget(Guid widgetId, int newCol, int newRow)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null) return;

        ValidatePosition(newCol, newRow);

        OccupancyTracker.UnmarkOccupied(
            widget.Layout.Col, widget.Layout.Row,
            widget.Layout.Width, widget.Layout.Height);

        var collisions = FindCollidingWidgets(
            newCol, newRow, widget.Layout.Width, widget.Layout.Height, widgetId);

        if (collisions.Count > 0)
        {
            int firstColliderTop = collisions[0].Layout.Row;
            int intrusionDepth = (newRow + widget.Layout.Height) - firstColliderTop;
            CascadeWidgetsDown(collisions, intrusionDepth);
        }

        var newLayout = widget.Layout.WithPosition(newCol, newRow);
        widget.UpdateLayout(newLayout);
        OccupancyTracker.MarkOccupied(newCol, newRow, newLayout.Width, newLayout.Height);

        UpdateTimestamp();
    }

    public void ResizeWidget(Guid widgetId, int newWidth, int newHeight)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null) return;

        ValidateWidgetDimensions(newWidth, newHeight);

        int heightDelta = newHeight - widget.Layout.Height;

        if (heightDelta > 0)
        {
            int expansionStartRow = widget.Layout.Row + widget.Layout.Height;
            var collisions = FindCollidingWidgets(
                widget.Layout.Col, expansionStartRow,
                newWidth, heightDelta, widgetId);

            if (collisions.Count > 0)
                CascadeWidgetsDown(collisions, heightDelta);
        }

        var oldLayout = widget.Layout;
        var newLayout = new WidgetLayout(oldLayout.Col, oldLayout.Row, newWidth, newHeight);

        OccupancyTracker.UnmarkOccupied(
            oldLayout.Col, oldLayout.Row,
            oldLayout.Width, oldLayout.Height);

        widget.UpdateLayout(newLayout);

        OccupancyTracker.MarkOccupied(
            newLayout.Col, newLayout.Row,
            newLayout.Width, newLayout.Height);

        UpdateTimestamp();
    }

    private List<Widget> FindCollidingWidgets(
        int col, int row, int width, int height, Guid excludeId)
    {
        return _widgets
            .Where(w =>
                w.Id != excludeId
                && w.Layout.Col < col + width
                && w.Layout.Col + w.Layout.Width > col
                && w.Layout.Row < row + height
                && w.Layout.Row + w.Layout.Height > row)
            .OrderBy(w => w.Layout.Row)
            .ToList();
    }

    private void CascadeWidgetsDown(List<Widget> widgets, int pushDistance)
    {
        if (widgets.Count == 0) return;
        foreach (var w in widgets)
        {
            int newRow = w.Layout.Row + pushDistance;
            OccupancyTracker.UnmarkOccupied(
                w.Layout.Col, w.Layout.Row,
                w.Layout.Width, w.Layout.Height);

            var pushed = w.Layout.WithPosition(w.Layout.Col, newRow);
            w.UpdateLayout(pushed);

            OccupancyTracker.MarkOccupied(
                pushed.Col, pushed.Row,
                pushed.Width, pushed.Height);
        }
    }

    private WidgetLayout FindNextAvailablePosition(int width, int height)
    {
        int scanLimit = Math.Min(
            OccupancyTracker.GetScanUpperBound(height), MaxScanRows);

        for (int row = 0; row < scanLimit; row++)
            for (int col = 0; col <= MaxGridCols - width; col++)
                if (OccupancyTracker.CanPlaceAt(col, row, width, height))
                    return new WidgetLayout(col, row, width, height);

        throw new InvalidOperationException("Dashboard grid is full.");
    }

    private static void ValidateWidgetDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Widget dimensions must be positive.");
    }

    private static void ValidatePosition(int col, int row)
    {
        if (col < 0 || row < 0)
            throw new ArgumentException("Position cannot be negative.");
    }
    ────────────────────────────────────────────────────────────────── */
}