import { useState } from "react";
import * as Icons from "lucide-react";
import React from "react";
import { Input } from "./ui/input";
import { cn } from "@/lib/utils";
import { Popover, PopoverContent, PopoverTrigger } from "./ui/popover";
import { Button } from "./ui/button";

interface Props {
  value: string;
  onChange: (newValue: string) => void;
}
const IconPicker = ({ value, onChange }: Props) => {
  const [searchTerm, setSearchTerm] = useState("");

  const iconNames = React.useMemo(() => {
    return Object.keys(Icons)
      .filter((name) => name !== "createLucideIcon" && name !== "default")
      .filter((name) => name.toLowerCase().includes(searchTerm.toLowerCase()))
      .slice(0, 50);
  }, [searchTerm]);

  const SelectedIcon = (Icons as any)[value] || Icons.HelpCircle;

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          className="w-full justify-start gap-2 h-10 px-3 font-mono text-xs uppercase"
        >
          <SelectedIcon size={16} className="shrink-0" />
          {value || "Select Icon"}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="p-4 w-72" align="start">
        <div className="flex flex-col">
          {/* Search Bar */}
          <Input
            placeholder="Search icons..."
            className="w-full p-2 mb-4 border border-border rounded-md text-sm bg-background"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />

          {/* Icon Grid */}
          <div className="grid grid-cols-4 gap-2 max-h-48 overflow-y-auto p-1 scrollbar-thin">
            {iconNames.map((name) => {
              const Icon = (Icons as any)[name];
              const isSelected = value === name;
              return (
                <button
                  key={name}
                  type="button"
                  onClick={(e) => {
                    e.preventDefault();
                    onChange(name);
                  }}
                  className={cn(
                    "p-2 rounded-md flex items-center justify-center transition-all hover:scale-110",
                    isSelected
                      ? "bg-primary text-primary-foreground ring-2 ring-primary ring-offset-2 ring-offset-background"
                      : "bg-muted hover:bg-accent text-muted-foreground hover:text-accent-foreground"
                  )}
                  title={name}
                >
                  <Icon size={20} />
                </button>
              );
            })}
          </div>

          <div className="mt-3 text-[10px] text-muted-foreground text-center font-mono">
            Selected:{" "}
            <span className="text-foreground uppercase">{value || "None"}</span>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
};

export default IconPicker;
