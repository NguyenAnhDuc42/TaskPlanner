import { 
  Globe, 
  Lock, 
  Calendar as CalendarIcon, 
} from "lucide-react";
import * as Icons from "lucide-react";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { UniversalPicker } from "@/components/universal-picker";

// --- PRIVACY TOGGLE ---
export function PrivacyToggle({ isPrivate, onChange }: { isPrivate: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex bg-muted/30 p-0.5 rounded-lg border border-border/50 w-fit">
      <button
        type="button"
        onClick={() => onChange(false)}
        className={cn(
          "flex items-center gap-1.5 px-3 py-1 rounded-md text-[10px] font-bold uppercase tracking-tight transition-all",
          !isPrivate ? "bg-background shadow-sm text-primary" : "text-muted-foreground hover:text-foreground"
        )}
      >
        <Globe className="h-3 w-3" />
        Public
      </button>
      <button
        type="button"
        onClick={() => onChange(true)}
        className={cn(
          "flex items-center gap-1.5 px-3 py-1 rounded-md text-[10px] font-bold uppercase tracking-tight transition-all",
          isPrivate ? "bg-background shadow-sm text-primary" : "text-muted-foreground hover:text-foreground"
        )}
      >
        <Lock className="h-3 w-3" />
        Private
      </button>
    </div>
  );
}

// --- ICON & COLOR PICKER (Wrapper for UniversalPicker) ---
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
        <Button 
          variant="ghost" 
          size="icon" 
          className="h-8 w-8 rounded-md bg-muted/30 hover:bg-muted/50 border border-border/50 shrink-0"
          style={{ color: color }}
        >
          <SelectedIcon className="h-4 w-4" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-fit p-0 border-none shadow-none bg-transparent" align="start">
        <UniversalPicker 
          selectedIcon={icon} 
          selectedColor={color} 
          onSelect={onChange} 
        />
      </PopoverContent>
    </Popover>
  );
}

// --- DATE PICKER (Mock for now) ---
export function SimpleDatePicker({ value, onChange }: { value?: Date; onChange: (d: Date) => void }) {
  return (
    <Button 
      variant="ghost" 
      size="sm" 
      className="h-8 px-3 rounded-md bg-muted/30 hover:bg-muted/50 border border-border/50 text-[10px] font-bold uppercase tracking-tight text-muted-foreground"
    >
      <CalendarIcon className="h-3 w-3 mr-2" />
      {value ? value.toLocaleDateString() : "Schedule"}
    </Button>
  );
}
