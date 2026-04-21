export const StatusCategory = {
  NotStarted: "NotStarted",
  Active: "Active",
  Done: "Done",
  Closed: "Closed",
} as const;

export type StatusCategory = (typeof StatusCategory)[keyof typeof StatusCategory];