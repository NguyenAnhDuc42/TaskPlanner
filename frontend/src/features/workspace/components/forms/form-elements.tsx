import React, { forwardRef } from "react";
import { 
  Globe, 
  Lock, 
  Calendar as CalendarIcon, 
} from "lucide-react";
import * as Icons from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { UniversalPicker } from "@/components/universal-picker";
import { Calendar } from "@/components/ui/calendar";

// --- ATTRIBUTE BUTTON ---
export const AttributeButton = forwardRef<
  HTMLButtonElement,
  { 
    children: React.ReactNode; 
    icon?: any; 
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

// --- PRIVACY TOGGLE ---
export function PrivacyToggle({ isPrivate, onChange }: { isPrivate: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex bg-muted/20 p-0.5 rounded-lg border border-border/10 w-fit">
      <button
        type="button"
        onClick={() => onChange(false)}
        className={cn(
          "flex items-center gap-1.5 px-2 h-5 rounded-[5px] text-[9px] font-medium transition-all",
          !isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/20" : "text-muted-foreground hover:text-foreground"
        )}
      >
        <Globe className="h-2.5 w-2.5" />
        Public
      </button>
      <button
        type="button"
        onClick={() => onChange(true)}
        className={cn(
          "flex items-center gap-1.5 px-2 h-5 rounded-[5px] text-[9px] font-medium transition-all",
          isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/20" : "text-muted-foreground hover:text-foreground"
        )}
      >
        <Lock className="h-2.5 w-2.5" />
        Private
      </button>
    </div>
  );
}

// --- ICON & COLOR PICKER ---
export function IconColorPicker({ 
  icon, 
  color, 
  onChange 
}: { 
  icon: string; 
  color: string; 
  onChange: (icon: string, color: string) => void 
}) {
  const SelectedIcon = (Icons as any)[icon] || Icons.LayoutGrid;

  return (
    <Popover>
      <PopoverTrigger asChild>
        <button 
          type="button"
          className="h-6 w-6 rounded-md hover:bg-muted/50 flex items-center justify-center transition-colors shrink-0"
          style={{ color: color }}
        >
          <SelectedIcon className="h-4 w-4" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-fit p-0 border border-border/50 shadow-xl rounded-xl bg-background overflow-hidden" align="start" sideOffset={8}>
        <UniversalPicker 
          selectedIcon={icon} 
          selectedColor={color} 
          onSelect={onChange} 
        />
      </PopoverContent>
    </Popover>
  );
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
          initialFocus
        />
      </PopoverContent>
    </Popover>
  );
}
