import { RichTextEditor } from "@/components/rich-text-editor";
import { useState, useEffect } from "react";

interface DescriptionSectionProps {
  initialValue?: string;
  onChange?: (value: string) => void;
}

export function DescriptionSection({ initialValue = "", onChange }: DescriptionSectionProps) {
  const [content, setContent] = useState(initialValue);

  // Sync from props only when the value actually changes from external source
  useEffect(() => {
    if (initialValue !== content) {
      setContent(initialValue);
    }
  }, [initialValue]);

  const handleChange = (val: string) => {
    setContent(val);
    onChange?.(val);
  };

  return (
    <div className="relative group">
      <div className="min-h-[400px]">
        <RichTextEditor
          value={content}
          onChange={handleChange}
          placeholder="Define the strategic objectives and operational scope for this layer..."
        />
      </div>
    </div>
  );
}
