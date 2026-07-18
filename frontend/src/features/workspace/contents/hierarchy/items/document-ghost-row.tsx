import { useEffect, useRef, useState } from "react";
import { FileText } from "lucide-react";

interface DocumentGhostRowProps {
  depth?: number;
  onCommit: (name: string) => void;
  onCancel: () => void;
}

// A pending-create row: shows an input in place of the usual name button, and only actually
// creates the document once a non-empty name is committed (Enter/blur) — never on click alone.
export function DocumentGhostRow({ depth = 0, onCommit, onCancel }: DocumentGhostRowProps) {
  const [name, setName] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const settledRef = useRef(false);

  useEffect(() => {
    const raf = requestAnimationFrame(() => inputRef.current?.focus());
    return () => cancelAnimationFrame(raf);
  }, []);

  const settle = (commit: boolean) => {
    if (settledRef.current) return;
    settledRef.current = true;
    const trimmed = name.trim();
    if (commit && trimmed) onCommit(trimmed);
    else onCancel();
  };

  return (
    <div
      className="flex items-center px-1 py-0.5 rounded-md mb-px border border-primary/40 bg-primary/5"
      style={{ paddingLeft: 4 + depth * 14 }}
    >
      <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-1">
        <FileText className="h-3.5 w-3.5 opacity-60" />
      </div>
      <input
        ref={inputRef}
        value={name}
        onChange={(e) => setName(e.target.value)}
        onBlur={() => settle(true)}
        onKeyDown={(e) => {
          if (e.key === "Enter") e.currentTarget.blur();
          if (e.key === "Escape") settle(false);
        }}
        placeholder="Page name..."
        className="flex-1 text-[11px] font-semibold bg-transparent border-none outline-none placeholder:text-muted-foreground/40"
      />
    </div>
  );
}
