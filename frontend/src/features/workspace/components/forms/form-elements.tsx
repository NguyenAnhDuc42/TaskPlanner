import React, { forwardRef } from "react";
import {
  Calendar as CalendarIcon,
} from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { UniversalPicker } from "@/components/universal-picker";
import { Calendar } from "@/components/ui/calendar";

// --- ATTRIBUTE BUTTON ---
export const AttributeButton = forwardRef<
  HTMLButtonElement,
  { 
    children: React.ReactNode; 
    icon?: React.ElementType; 
    className?: string;
    active?: boolean;
    onClick?: () => void;
  }
>(({ children, icon: Icon, className, active, onClick, ...props }, ref) => {
  return (
    <button
      ref={ref}
      type="button"
      onClick={onClick}
      className={cn(
        "flex items-center gap-1.5 px-2 h-6 rounded-md border border-transparent",
        "text-[10px] font-medium transition-all duration-200 whitespace-nowrap",
        "bg-muted/30 hover:bg-muted/50",
        active ? "text-foreground bg-muted/60 border-border/20" : "text-muted-foreground",
        className
      )}
      {...props}
    >
      {Icon && <Icon className="h-3 w-3 shrink-0" />}
      {children}
    </button>
  );
});
AttributeButton.displayName = "AttributeButton";


// --- ICON & COLOR PICKER ---
export function IconColorPicker({
  icon,
  color,
  onChange,
}: Readonly<{ icon: string; color: string; onChange: (icon: string, color: string) => void }>) {
  return <UniversalPicker icon={icon} color={color} onSelect={onChange} size="md" />;
}

// --- DATE PICKER ---
export function SimpleDatePicker({ value, onChange, label = "Due Date" }: { value?: Date; onChange: (d: Date | undefined) => void; label?: string }) {
  return (
    <Popover>
      <PopoverTrigger asChild>
        <AttributeButton icon={CalendarIcon} active={!!value}>
          {value ? value.toLocaleDateString() : label}
        </AttributeButton>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0 border border-border/50 shadow-xl rounded-xl bg-background overflow-hidden" align="start">
        <Calendar
          mode="single"
          selected={value}
          onSelect={onChange}
        />
      </PopoverContent>
    </Popover>
  );
}
