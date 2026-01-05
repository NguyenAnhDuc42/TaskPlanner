export type Role = "None" | "Owner" | "Admin" | "Member" | "Guest";

export const ROLE_LABELS: Record<Role, string> = {
  None : "None",
  Owner: "Owner",
  Admin: "Admin",
  Member: "Member",
  Guest: "Guest",
}

export function getRoleLabel(role: Role) {
  return ROLE_LABELS[role] ?? role;
}