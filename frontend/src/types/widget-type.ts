export const WidgetType = {
  TaskList: "TaskList",
  Metric: "Metric",
  Chart: "Chart",
} as const;

export type WidgetType = (typeof WidgetType)[keyof typeof WidgetType];
