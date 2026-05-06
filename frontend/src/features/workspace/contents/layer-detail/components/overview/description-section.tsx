import { RichTextEditor } from "@/components/rich-text-editor";
import { Save } from "lucide-react";
import { useState, useEffect } from "react";

interface DescriptionSectionProps {
  initialValue?: string;
  onSave?: (value: string) => void;
}

export function DescriptionSection({ initialValue = "", onSave }: DescriptionSectionProps) {
  const [content, setContent] = useState(initialValue);
  const [isSaving, setIsSaving] = useState(false);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);

  useEffect(() => {
    if (content === initialValue) return;

    const timer = setTimeout(() => {
      setIsSaving(true);
      setTimeout(() => {
        setIsSaving(false);
        setLastSaved(new Date());
        onSave?.(content);
      }, 800);
    }, 2000);

    return () => clearTimeout(timer);
  }, [content, initialValue, onSave]);

  return (
    <div className="relative group">
      {/* Absolute Auto-save Indicator */}
      <div className="absolute -top-6 right-0">
        <div className="flex items-center gap-2 text-[9px] font-bold uppercase tracking-wider transition-all duration-300 min-h-[14px]">
          {isSaving ? (
            <div className="flex items-center gap-1.5 text-primary/60 animate-in fade-in slide-in-from-right-1 duration-300">
              <div className="h-1 w-1 rounded-full bg-primary animate-pulse" />
              <span>Saving Changes...</span>
            </div>
          ) : lastSaved ? (
            <div className="flex items-center gap-1.5 text-muted-foreground/15 group-hover:text-muted-foreground/30 transition-colors animate-in fade-in duration-500">
              <Save className="h-2.5 w-2.5" />
              <span>Saved {lastSaved.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
            </div>
          ) : null}
        </div>
      </div>

      <div className="min-h-[400px]">
        <RichTextEditor
          value={content}
          onChange={setContent}
          placeholder="Define the strategic objectives and operational scope for this layer..."
        />
      </div>
    </div>
  );
}
