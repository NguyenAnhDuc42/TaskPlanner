import { Input } from "./ui/input";

interface Props {
  value: string;
  onChange: (newValue: string) => void;
}

export const ColorPicker = ({ value, onChange }: Props) => {
 return (
    <div className="flex items-center gap-3">
      {/* Visual Preview / Hidden Input Trigger */}
      <div className="relative group shrink-0">
        <div 
          className="w-10 h-10 rounded-md border-2 border-background shadow-sm ring-1 ring-border group-hover:ring-primary transition-all cursor-pointer"
          style={{ backgroundColor: value }}
        />
        <input
          type="color"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
        />
      </div>

      {/* Hex Text Input */}
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="#000000"
        className="font-mono text-xs uppercase h-10"
        maxLength={7}
      />
    </div>
  )
};
