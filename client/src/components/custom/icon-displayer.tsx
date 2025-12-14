import React from "react";
import * as LucideIcons from "lucide-react";
import { cn } from "@/lib/utils";
import { HelpCircle } from "lucide-react"; // Import HelpCircle

interface IconDisplayerProps {
  iconName: string;
  backgroundColor: string;
  className?: string;
}

export const IconDisplayer: React.FC<IconDisplayerProps> = ({ iconName, backgroundColor, className }) => {
  const IconComponent = LucideIcons[iconName as keyof typeof LucideIcons] as React.ElementType;

  if (!IconComponent) {
    console.warn(`Icon "${iconName}" not found in LucideIcons. Displaying default HelpCircle icon.`);
    return (
      <div
        className={cn("size-4 rounded text-xs flex items-center justify-center text-white font-medium", className)}
        style={{ backgroundColor: backgroundColor }}
      >
        <HelpCircle className="size-3" /> {/* Default icon */}
      </div>
    );
  }

  return (
    <div
      className={cn("size-4 rounded text-xs flex items-center justify-center text-white font-medium", className)}
      style={{ backgroundColor: backgroundColor }}
    >
      <IconComponent className="size-3" />
    </div>
  );
};
