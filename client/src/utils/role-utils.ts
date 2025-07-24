export type Role = "owner" | "admin" | "member" | "guest"

/**
 * Maps numeric role IDs from the API to client-side Role strings.
 * NOTE: Adjust the numbers if they don't match your backend's enum.
 */
export const ROLE_MAP: Record<number, Role> = {
  0: "owner",
  1: "admin",
  2: "member",
  3: "guest",
}

export function mapRoleFromApi(roleId: number): Role {
  return ROLE_MAP[roleId] ?? "guest" // Default to 'guest' if ID is unknown
}
export function getRoleProperties(role: Role) {
  const properties = {
    guest: {
      label: "Guest",
      color: "bg-gray-500 text-white",
    },
    member: {
      label: "Member",
      color: "bg-green-600 text-white",
    },
    admin: {
      label: "Admin",
      color: "bg-purple-600 text-white",
    },
    owner: {
      label: "Owner",
      color: "bg-blue-600 text-white",
    },
  }
  return properties[role]
}

export function getAssignableRoles(currentRole: Role): Role[] {
  switch (currentRole) {
    case "owner":
      return ["owner", "admin", "member", "guest"]
    case "admin":
      return ["admin", "member", "guest"]
    case "member":
      return ["member", "guest"]
    case "guest":
      return ["guest"]
    default:
      return []
  }
}

export function canManageRole(currentRole: Role, targetRole: Role): boolean {
  const roleHierarchy = {
    owner: 4,
    admin: 3,
    member: 2,
    guest: 1,
  }

  return (roleHierarchy[currentRole] || 0) > (roleHierarchy[targetRole] || 0)
}
