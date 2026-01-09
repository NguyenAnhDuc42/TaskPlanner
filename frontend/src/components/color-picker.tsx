import * as React from "react";
import { Input } from "./ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "./ui/popover";
import { Button } from "./ui/button";
import { cn } from "@/lib/utils";

interface Props {
  value: string;
  onChange: (newValue: string) => void;
}

export const ColorPicker = ({ value, onChange }: Props) => {
  const [localValue, setLocalValue] = React.useState(value);

  React.useEffect(() => {
    setLocalValue(value);
  }, [value]);

  const handleApply = (val: string) => {
    if (val !== value) {
      onChange(val);
    }
  };

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          className="w-full justify-start gap-2 h-10 px-3 font-mono text-xs uppercase"
        >
          <div
            className="w-4 h-4 rounded-full border border-border shrink-0"
            style={{ backgroundColor: localValue }}
          />
          {localValue.substring(0, 7)}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-3" align="start">
        <div className="flex flex-col gap-3">
          <div className="flex items-center gap-3">
            <div className="relative shrink-0 overflow-hidden rounded-md border border-border w-10 h-10">
              <div
                className="absolute inset-0"
                style={{ backgroundColor: localValue }}
              />
              <input
                type="color"
                value={localValue}
                onInput={(e) =>
                  setLocalValue((e.target as HTMLInputElement).value)
                }
                onChange={(e) => handleApply(e.target.value)}
                className="absolute inset-0 w-full h-full opacity-0 cursor-pointer scale-150"
              />
            </div>
            <Input
              value={localValue.substring(0, 7)}
              onChange={(e) => setLocalValue(e.target.value)}
              onBlur={(e) => handleApply(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  handleApply((e.target as HTMLInputElement).value);
                }
              }}
              placeholder="#000000"
              className="font-mono text-xs uppercase h-10 w-28"
              maxLength={7}
            />
          </div>
          <div className="grid grid-cols-5 gap-1.5">
            {["#4f46e5", "#0ea5e9", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6"
              , "#ec4899","#6366f1", "#14b8a6", "#f43f5e"].map((c) => (
              <button
                key={c}
                type="button"
                className={cn(
                  "w-6 h-6 rounded-full border border-border transition-transform hover:scale-110",
                  localValue === c &&
                    "ring-2 ring-primary ring-offset-2 ring-offset-background"
                )}
                style={{ backgroundColor: c }}
                onClick={() => {
                  setLocalValue(c);
                  handleApply(c);
                }}
              />
            ))}
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
};
