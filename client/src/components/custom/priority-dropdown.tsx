"use client"

import { useState } from "react"
import { ChevronDown, Flag, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Priority } from "@/utils/priority-utils"

interface PriorityDropdownProps {
  priority: Priority | null
  onPriorityChange?: (priority: Priority | null) => void
  disabled?: boolean
}

export function PriorityDropdown({ priority, onPriorityChange, disabled = false }: PriorityDropdownProps) {
  const [isOpen, setIsOpen] = useState(false)

  const priorityConfig = {
    [Priority.Urgent]: {
      text: "Urgent",
      flagColor: "text-red-500",
      bgColor: "bg-red-500/10",
    },
    [Priority.High]: {
      text: "High",
      flagColor: "text-yellow-500",
      bgColor: "bg-yellow-500/10",
    },
    [Priority.Medium]: {
      text: "Normal",
      flagColor: "text-blue-500",
      bgColor: "bg-blue-500/10",
    },
    [Priority.Low]: {
      text: "Low",
      flagColor: "text-gray-500",
      bgColor: "bg-gray-500/10",
    },
    [Priority.Clear]: {
      text: "",
      flagColor: "text-gray-400",
      bgColor: "bg-transparent",
    },
  }

  const currentConfig = priority ? priorityConfig[priority] : priorityConfig[Priority.Clear]

  const handlePrioritySelect = (newPriority: Priority | null) => {
    onPriorityChange?.(newPriority)
    setIsOpen(false)
  }

  if (disabled) {
    return (
      <div className={`flex items-center gap-1.5 px-2 py-1 rounded text-xs ${currentConfig.bgColor}`}>
        <Flag className={`h-3 w-3 ${currentConfig.flagColor}`} />
        {currentConfig.text && <span className="text-gray-300">{currentConfig.text}</span>}
      </div>
    )
  }

  return (
    <div className="relative">
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setIsOpen(!isOpen)}
        className={`flex items-center gap-1.5 px-2 py-1 h-auto text-xs hover:bg-gray-700 ${currentConfig.bgColor}`}
      >
        <Flag className={`h-3 w-3 ${currentConfig.flagColor}`} />
        {currentConfig.text && <span className="text-gray-300">{currentConfig.text}</span>}
        {isOpen ? <X className="h-3 w-3 text-gray-400" /> : <ChevronDown className="h-3 w-3 text-gray-400" />}
      </Button>

      {isOpen && (
        <div className="absolute top-full left-0 mt-1 bg-gray-800 border border-gray-600 rounded-md shadow-lg z-50 min-w-[120px]">
          {Object.values(Priority).map((priorityOption) => {
            const config = priorityConfig[priorityOption]
            return (
              <button
                key={priorityOption}
                onClick={() => handlePrioritySelect(priorityOption)}
                className={`w-full flex items-center gap-2 px-3 py-2 text-xs hover:bg-gray-700 first:rounded-t-md last:rounded-b-md ${config.bgColor}`}
              >
                <Flag className={`h-3 w-3 ${config.flagColor}`} />
                <span className="text-gray-300">{config.text || "Clear"}</span>
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}
