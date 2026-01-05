import { useState } from "react";
import * as Icons from "lucide-react";
import React from "react";
import { Input } from "./ui/input";

interface Props {
  value: string;
  onChange: (newValue: string) => void;
}
const IconPicker = ({ value, onChange }: Props) => {
  const [searchTerm, setSearchTerm] = useState("");

  // Get all icon names from the Lucide object
  const iconNames = React.useMemo(() => {
    return Object.keys(Icons)
      .filter((name) => name !== "createLucideIcon" && name !== "default")
      .filter((name) => name.toLowerCase().includes(searchTerm.toLowerCase()))
      .slice(0, 50);
  }, [searchTerm]);

  return (
    <div className="p-4 border rounded-xl bg-white w-72">
      {/* Search Bar */}
      <Input
        placeholder="Search icons..."
        className="w-full p-2 mb-4 border rounded-md text-sm"
        onChange={(e) => setSearchTerm(e.target.value)}
      />

      {/* Icon Grid */}
      <div className="grid grid-cols-4 gap-2 max-h-48 overflow-y-auto p-1">
        {iconNames.map((name) => {
          const Icon = (Icons as any)[name]
          return (
            <button
              key={name}
              onClick={() => onChange(name)}
              className={`p-2 rounded-md flex items-center justify-center hover:bg-blue-50 transition-colors ${
                value === name
                  ? "bg-blue-100 ring-2 ring-blue-500"
                  : "bg-gray-50"
              }`}
              title={name}
            >
              <Icon size={20} color={value === name ? "#2563eb" : "#64748b"} />
            </button>
          );
        })}
      </div>

      <div className="mt-3 text-xs text-gray-500 text-center">
        Selected: <strong>{value || "None"}</strong>
      </div>
    </div>
  );
};

export default IconPicker;
