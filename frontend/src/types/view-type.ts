export const ViewType = {
  List: "List",
  Board: "Board",
  Calendar: "Calendar",
  Dashboard: "Dashboard",
  Doc: "Doc",
} as const;

export type ViewType = (typeof ViewType)[keyof typeof ViewType];
