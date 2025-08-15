import { Badge } from "@/components/ui/badge"
import { Priority } from "@/utils/priority-utils"
import { cn } from "@/lib/utils"
import { Flag } from "lucide-react"

interface PriorityBadgeProps {
  priority: Priority | null
  size?: "xs" | "sm" | "md" | "lg"
  className?: string
}

export function PriorityBadge({ priority, size = "md", className }: PriorityBadgeProps) {
  const sizeClasses = {
    xs: "text-[0.65rem] px-1.5 py-0.5",
    sm: "text-xs px-2 py-0.5",
    md: "text-sm px-2.5 py-1",
    lg: "text-base px-3 py-1.5",
  }

  const priorityConfig = {
    [Priority.Urgent]: {
      text: "Urgent",
      classes: "bg-red-500 text-white border-red-500",
      iconColor: "text-white",
    },
    [Priority.High]: {
      text: "High",
      classes: "bg-yellow-500 text-black border-yellow-500",
      iconColor: "text-black",
    },
    [Priority.Medium]: {
      text: "Normal",
      classes: "bg-blue-500 text-white border-blue-500",
      iconColor: "text-white",
    },
    [Priority.Low]: {
      text: "Low",
      classes: "bg-gray-600 text-white border-gray-600",
      iconColor: "text-white",
    },
    [Priority.Clear]: {
      text: "",
      classes: "bg-transparent text-gray-400 border-gray-400 border-dashed",
      iconColor: "text-gray-400",
    },
  }

  const config = priority ? priorityConfig[priority] : priorityConfig[Priority.Clear]

  return (
    <Badge
      className={cn(
        config.classes,
        sizeClasses[size],
        "font-medium transition-colors flex items-center gap-1.5",
        className,
      )}
    >
      <Flag className={cn("h-3 w-3", config.iconColor)} />
      {config.text && <span>{config.text}</span>}
    </Badge>
  )
}