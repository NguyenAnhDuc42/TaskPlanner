import { useState, useRef, useEffect } from "react";
import { cn } from "@/lib/utils";
import { Priority } from "@/types/priority";

interface InlinePriorityPickerProps {
  priority?: Priority | "no-priority";
  onPriorityChange: (priority: Priority) => void;
  className?: string;
}

const PRIORITY_OPTIONS: { value: Priority; label: string; dot: string; bg: string; border: string; text: string }[] = [
  {
    value: Priority.Urgent,
    label: "Urgent",
    dot: "bg-red-500 shadow-[0_0_6px_#ef4444]",
    bg: "hover:bg-red-500/10",
    border: "border-red-500/20",
    text: "text-red-400/90",
  },
  {
    value: Priority.High,
    label: "High",
    dot: "bg-orange-500",
    bg: "hover:bg-orange-500/10",
    border: "border-orange-500/20",
    text: "text-orange-400/90",
  },
  {
    value: Priority.Normal,
    label: "Normal",
    dot: "bg-blue-500",
    bg: "hover:bg-blue-500/10",
    border: "border-blue-500/20",
    text: "text-blue-400/90",
  },
  {
    value: Priority.Low,
    label: "Low",
    dot: "bg-muted-foreground/20",
    bg: "hover:bg-white/[0.03]",
    border: "border-white/[0.04]",
    text: "text-muted-foreground/50",
  },
];

function getBadgeStyles(priority?: Priority | "no-priority") {
  switch (priority) {
    case Priority.Urgent:
      return "bg-red-500/10 border-red-500/20 text-red-400/90";
    case Priority.High:
      return "bg-orange-500/10 border-orange-500/20 text-orange-400/90";
    case Priority.Normal:
      return "bg-blue-500/10 border-blue-500/20 text-blue-400/90";
    default:
      return "bg-white/[0.02] border-white/[0.04] text-muted-foreground/30";
  }
}

function getDotStyles(priority?: Priority | "no-priority") {
  switch (priority) {
    case Priority.Urgent:
      return "bg-red-500 shadow-[0_0_6px_#ef4444]";
    case Priority.High:
      return "bg-orange-500";
    case Priority.Normal:
      return "bg-blue-500";
    default:
      return "bg-muted-foreground/20";
  }
}

export function InlinePriorityPicker({
  priority,
  onPriorityChange,
  className,
}: InlinePriorityPickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  const displayPriority = priority === "no-priority" ? undefined : priority;

  return (
    <div ref={containerRef} className={cn("relative inline-flex", className)}>
      {/* Trigger: styled like PriorityBadge */}
      <button
        type="button"
        onClick={(e) => {
          e.stopPropagation();
          e.preventDefault();
          setIsOpen((prev) => !prev);
        }}
        className={cn(
          "flex items-center gap-1 px-1.5 py-0.5 rounded-md border text-[8px] font-black uppercase tracking-[0.1em] transition-all duration-200 cursor-pointer",
          "hover:ring-1 hover:ring-white/10",
          getBadgeStyles(displayPriority),
        )}
      >
        <div className={cn("h-1 w-1 rounded-full", getDotStyles(displayPriority))} />
        {displayPriority || "Normal"}
      </button>

      {/* Dropdown */}
      {isOpen && (
        <div
          className="absolute z-50 top-full left-0 mt-1 min-w-[120px] rounded-lg border border-[#2c2d35] bg-[#141416] shadow-2xl shadow-black/50 py-1 animate-in fade-in slide-in-from-top-1 duration-150"
          onClick={(e) => e.stopPropagation()}
        >
          {PRIORITY_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                onPriorityChange(opt.value);
                setIsOpen(false);
              }}
              className={cn(
                "w-full flex items-center gap-2 px-3 py-1.5 text-[10px] font-bold uppercase tracking-wider transition-colors cursor-pointer",
                opt.bg,
                opt.text,
                displayPriority === opt.value && "bg-white/[0.04]",
              )}
            >
              <div className={cn("h-1.5 w-1.5 rounded-full shrink-0", opt.dot)} />
              {opt.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
