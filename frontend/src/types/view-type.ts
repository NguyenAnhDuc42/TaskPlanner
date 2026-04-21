export const ViewType = {
  Overview: "Overview",
  Task: "Task",
} as const;

export type ViewType = (typeof ViewType)[keyof typeof ViewType];
