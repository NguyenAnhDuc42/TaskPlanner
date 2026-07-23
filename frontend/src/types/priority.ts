export const Priority = {
  None: "None",
  Low: "Low",
  Normal: "Normal",
  High: "High",
  Urgent: "Urgent",
} as const;

export type Priority = (typeof Priority)[keyof typeof Priority];

export const PriorityWeight: Record<Priority, number> = {
  [Priority.Urgent]: 4,
  [Priority.High]: 3,
  [Priority.Normal]: 2,
  [Priority.Low]: 1,
  [Priority.None]: 0,
};

export const WeightToPriority: Record<number, Priority> = {
  4: Priority.Urgent,
  3: Priority.High,
  2: Priority.Normal,
  1: Priority.Low,
  0: Priority.None,
};

export function getPriorityWeight(item: { priority?: string; orderKey?: string } | null | undefined): number {
  if (!item || !item.priority) {
    return 0; // No priority set — sorts last
  }
  return PriorityWeight[item.priority as Priority] ?? 0;
}

export const prioritySort = (a: { id?: string; priority?: string; orderKey?: string }, b: { id?: string; priority?: string; orderKey?: string }) => {
  const pA = getPriorityWeight(a);
  const pB = getPriorityWeight(b);
  if (pA !== pB) {
    return pB - pA;
  }
  const ak = a.orderKey ?? "";
  const bk = b.orderKey ?? "";
  if (ak !== bk) return ak < bk ? -1 : 1;

  const aId = a.id ?? "";
  const bId = b.id ?? "";
  return aId < bId ? -1 : aId > bId ? 1 : 0;
};
