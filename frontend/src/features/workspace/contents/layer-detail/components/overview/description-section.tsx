import { RichTextEditor } from "@/components/rich-text-editor";
import { Save } from "lucide-react";
import { useState, useEffect, useRef } from "react";
import { useDebounce } from "@/hooks/use-debounce";

interface DescriptionSectionProps {
  initialValue?: string;
  onSave?: (value: string) => void;
}

export function DescriptionSection({ initialValue = "", onSave }: DescriptionSectionProps) {
  const [content, setContent] = useState(initialValue);
  const debouncedContent = useDebounce(content, 2000);
  const [isSaving, setIsSaving] = useState(false);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  
  // Use ref for onSave to keep it out of dependency arrays and avoid loops
  const onSaveRef = useRef(onSave);
  onSaveRef.current = onSave;

  // Sync from props only when the value actually changes from external source
  // and we are not currently "dirty" (editing)
  const isFirstRender = useRef(true);
  useEffect(() => {
    if (isFirstRender.current) {
      isFirstRender.current = false;
      return;
    }
    // Only sync if the new initialValue is different from our current content
    // and the server value just changed (e.g. from a refetch)
    if (initialValue !== content) {
      setContent(initialValue);
    }
  }, [initialValue]);

  // Handle Auto-save
  useEffect(() => {
    if (debouncedContent === initialValue) return;

    setIsSaving(true);
    const timer = setTimeout(() => {
      onSaveRef.current?.(debouncedContent);
      setIsSaving(false);
      setLastSaved(new Date());
    }, 500);

    return () => clearTimeout(timer);
  }, [debouncedContent, initialValue]);

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
