export const ASSIGNABLE_ACCESS_LEVELS = [
  "Manager",
  "Editor",
  "Viewer",
] as const;

export type AssignableAccessLevel = (typeof ASSIGNABLE_ACCESS_LEVELS)[number];
export type AccessLevel = "None" | AssignableAccessLevel;

export const ACCESS_LEVEL_LABELS: Record<AccessLevel, string> = {
  None: "None",
  Manager: "Manager",
  Editor: "Editor",
  Viewer: "Viewer",
};

export function getAccessLevelLabel(accessLevel: AccessLevel) {
  return ACCESS_LEVEL_LABELS[accessLevel] ?? accessLevel;
}

export function toAssignableAccessLevel(
  value: unknown,
  fallback: AssignableAccessLevel = "Editor",
): AssignableAccessLevel {
  if (value === null || value === undefined) return fallback;

  if (typeof value === "number") {
    if (value === 1) return "Manager";
    if (value === 2) return "Editor";
    if (value === 3) return "Viewer";
    return fallback;
  }

  if (typeof value !== "string") return fallback;

  const normalized = value.trim().toLowerCase();
  if (normalized === "1" || normalized === "manager") return "Manager";
  if (normalized === "2" || normalized === "editor") return "Editor";
  if (normalized === "3" || normalized === "viewer") return "Viewer";

  return fallback;
}
