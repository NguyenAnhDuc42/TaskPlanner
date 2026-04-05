import * as React from "react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

interface DropdownWrapperProps {
  trigger: React.ReactNode
  children: React.ReactNode
  align?: "start" | "center" | "end"
  side?: "top" | "bottom" | "left" | "right"
  className?: string
}

export function DropdownWrapper({ 
  trigger, 
  children, 
  align = "end", 
  side = "bottom",
  className 
}: DropdownWrapperProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {trigger}
      </DropdownMenuTrigger>
      <DropdownMenuContent align={align} side={side} className={className}>
        {children}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
