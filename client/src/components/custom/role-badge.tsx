import { Badge } from "@/components/ui/badge"
import { getRoleProperties, type Role } from "@/utils/role-utils" // Import Role type

interface RoleBadgeProps {
  role: Role // Now takes the Role string directly
  size?: "sm" | "md" | "lg"
}

export function RoleBadge({ role, size = "md" }: RoleBadgeProps) {
  // Get the display label and color using the utility
  const properties = getRoleProperties(role)

  // If no properties are found for the role, return null or a default badge
  if (!properties) {
    return null
  }

  const { label, color } = properties
  const sizeClasses = {
    sm: "text-xs px-2 py-0.5",
    md: "text-sm px-2.5 py-1",
    lg: "text-base px-3 py-1.5",
  }

  return <Badge className={`${color} ${sizeClasses[size]} font-medium`}>{label}</Badge>
}
