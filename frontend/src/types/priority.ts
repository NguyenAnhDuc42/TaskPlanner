export const Priority = {
  Low: "Low",
  Normal: "Normal",
  High: "High",
  Urgent: "Urgent",
} as const;

export type Priority = (typeof Priority)[keyof typeof Priority];
