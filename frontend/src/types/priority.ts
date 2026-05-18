export const Priority = {
  Low: "Low",
  Normal: "Normal",
  High: "High",
  Urgent: "Urgent",
} as const;

export type Priority = (typeof Priority)[keyof typeof Priority];

export const PriorityWeight: Record<Priority, number> = {
  [Priority.Urgent]: 3,
  [Priority.High]: 2,
  [Priority.Normal]: 1,
  [Priority.Low]: 0,
};

export const WeightToPriority: Record<number, Priority> = {
  3: Priority.Urgent,
  2: Priority.High,
  1: Priority.Normal,
  0: Priority.Low,
};

export function getPriorityWeight(item: any): number {
  if (!item || item.priority === "no-priority") {
    return 1; // Default Normal weight (1)
  }
  return PriorityWeight[item.priority as Priority] ?? 1;
}

export const prioritySort = (a: any, b: any) => {
  const pA = getPriorityWeight(a);
  const pB = getPriorityWeight(b);
  if (pA !== pB) {
    return pB - pA;
  }
  const ak = a.orderKey ?? "";
  const bk = b.orderKey ?? "";
  return ak < bk ? -1 : ak > bk ? 1 : 0;
};
