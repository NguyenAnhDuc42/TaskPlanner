export const StatusCategory = {
  NotStarted: "NotStarted",
  Active: "Active",
  Done: "Done",
  Closed: "Closed",
} as const;

export type StatusCategory =
  (typeof StatusCategory)[keyof typeof StatusCategory];

export interface StatusDto {
  id: string;
  name: string;
  color: string;
  category: StatusCategory;
  isDefault: boolean;
}
