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
      flagColor: "text-red-500",
      bgColor: "bg-transparent",
      textColor: "text-red-500",
    },
    [Priority.High]: {
      text: "High",
      flagColor: "text-yellow-500",
      bgColor: "bg-transparent",
      textColor: "text-yellow-500",
    },
    [Priority.Medium]: {
      text: "Normal",
      flagColor: "text-blue-500",
      bgColor: "bg-transparent",
      textColor: "text-blue-500",
    },
    [Priority.Low]: {
      text: "Low",
      flagColor: "text-gray-500",
      bgColor: "bg-transparent",
      textColor: "text-gray-500",
    },
    [Priority.Clear]: {
      text: "",
      flagColor: "text-gray-400",
      bgColor: "bg-transparent",
      textColor: "text-gray-400",
    },
  }

  const config = priority ? priorityConfig[priority] : priorityConfig[Priority.Clear]

  return (
    <div
      className={cn(
        "flex items-center gap-1.5 font-medium transition-colors min-w-16 justify-start",
        config.bgColor,
        sizeClasses[size],
        className,
      )}
    >
      <Flag className={cn("h-3 w-3 fill-current", config.flagColor)} />
      {config.text && <span className={config.textColor}>{config.text}</span>}
    </div>
  )
}
