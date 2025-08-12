import { useState } from "react";
import React from "react"; // Import React for React.memo
import { Popover, PopoverContent, PopoverTrigger } from "../ui/popover";
import { Button } from "../ui/button";
import { ScrollArea } from "../ui/scroll-area";

const colorOptions = [
  { name: "White", value: "#ffffff" },
  { name: "Light Gray", value: "#e5e7eb" },
  { name: "Gray", value: "#9ca3af" },
  { name: "Dark Gray", value: "#6b7280" },
  { name: "Slate", value: "#475569" },
  { name: "Charcoal", value: "#374151" },
  { name: "Dark Charcoal", value: "#1f2937" },
  { name: "Almost Black", value: "#111827" },
  { name: "Blue", value: "#3b82f6" },
  { name: "Indigo", value: "#6366f1" },
  { name: "Purple", value: "#8b5cf6" },
  { name: "Pink", value: "#ec4899" },
  { name: "Red", value: "#ef4444" },
  { name: "Orange", value: "#f97316" },
  { name: "Yellow", value: "#eab308" },
  { name: "Green", value: "#22c55e" },
  { name: "Teal", value: "#14b8a6" },
  { name: "Cyan", value: "#06b6d4" }
];

interface ColorPickerProps {
  value: string;
  onChange: (color: string) => void;
}

export const ColorPicker = React.memo(function ColorPicker({ value, onChange }: ColorPickerProps) {
  const [open, setOpen] = useState(false);

  const selectedColor = colorOptions.find(color => color.value === value);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <div
          role="button"
          tabIndex={0}
          aria-haspopup="dialog"
          className="w-full cursor-pointer flex items-center justify-start gap-2 bg-background border-border text-foreground hover:bg-accent hover:text-accent-foreground p-2 rounded"
          onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); setOpen(o => !o); } }}
          onClick={() => setOpen(o => !o)}
        >
          <div
            className="w-4 h-4 rounded-full border border-border"
            style={{ backgroundColor: value }}
          />
          <span>{selectedColor?.name || "Select Color"}</span>
        </div>
      </PopoverTrigger>
      <PopoverContent className="w-80 p-0 bg-popover border-border" align="start">
        <ScrollArea className="h-64">
          <div className="grid grid-cols-6 gap-2 p-4">
            {colorOptions.map((color) => (
              <Button
                key={color.value}
                variant="ghost"
                className={`h-10 w-10 p-0 rounded-full border-2 hover:scale-110 transition-transform ${
                  value === color.value ? "border-primary" : "border-border"
                }`}
                style={{ backgroundColor: color.value }}
                onClick={() => {
                  onChange(color.value);
                  setOpen(false);
                }}
                title={color.name}
              >
                <span className="sr-only">{color.name}</span>
              </Button>
            ))}
          </div>
        </ScrollArea>
      </PopoverContent>
    </Popover>
  );
});
