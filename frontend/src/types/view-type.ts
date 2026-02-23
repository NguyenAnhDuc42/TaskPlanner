export const ViewType = {
  List: 0,
  Board: 1,
  Calendar: 2,
  Dashboard: 3,
  Doc: 4,
} as const;

export type ViewType = (typeof ViewType)[keyof typeof ViewType];
