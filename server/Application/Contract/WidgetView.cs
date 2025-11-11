using System.Text.Json;

namespace Application.Contract;

public record WidgetViewDto(
    Guid WidgetId,
    string WidgetType,
    JsonDocument EffectiveConfig,          // merged config (override -> dashboard default -> widget config)
    WidgetLayoutDto Layout,
    bool Visible,
    int? PositionIndex,
    Guid? UserWidgetId,
    JsonDocument? SmallPayload             // small precomputed payload; optional
);
public record WidgetLayoutDto(int Col, int Row, int Width, int Height);