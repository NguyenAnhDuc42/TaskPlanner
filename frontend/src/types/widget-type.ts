export const WidgetType = {
  TaskList: "TaskList",
  FolderList: "FolderList",
  ActivityFeed: "ActivityFeed",
  NotificationSummary: "NotificationSummary",
  UpcomingDeadlines: "UpcomingDeadlines",
  WorkspaceHealth: "WorkspaceHealth",
  WorkloadSummary: "WorkloadSummary",
  GoalProgress: "GoalProgress",
  QuickActions: "QuickActions",
  Calendar: "Calendar",
  Hero: "Hero",
} as const;

export type WidgetType = (typeof WidgetType)[keyof typeof WidgetType];
