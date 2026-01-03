export type Role = string;

export const ROLE_LABELS: Record<string, string> = {
  None : "None",
  Owner: "Owner",
  Admin: "Admin",
  Member: "Member",
  Guest: "Guest",
}

export function getRoleLabel(role: Role) {
  return ROLE_LABELS[role] ?? role;
}