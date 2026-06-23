import { useState, useRef, useCallback, useEffect } from "react";
import { useSelector } from "react-redux";
import { memberSelectors } from "@/store/entityStore";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";

interface MentionInputProps {
  value: string;
  onChange: (v: string) => void;
  onSubmit: () => void;
  placeholder?: string;
  className?: string;
}

export function MentionInput({ value, onChange, onSubmit, placeholder, className }: MentionInputProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [mentionQuery, setMentionQuery] = useState<string | null>(null);
  const [mentionStart, setMentionStart] = useState<number>(-1);
  const [selectedIdx, setSelectedIdx] = useState(0);

  const allMembers = useSelector(memberSelectors.selectAll);

  const filtered = mentionQuery !== null
    ? allMembers.filter(m => m.name.toLowerCase().includes(mentionQuery.toLowerCase())).slice(0, 6)
    : [];

  const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const v = e.target.value;
    const cursor = e.target.selectionStart ?? v.length;
    onChange(v);

    // Detect @ trigger
    const before = v.slice(0, cursor);
    const atIdx = before.lastIndexOf("@");
    if (atIdx !== -1 && !before.slice(atIdx + 1).includes(" ")) {
      setMentionStart(atIdx);
      setMentionQuery(before.slice(atIdx + 1));
      setSelectedIdx(0);
    } else {
      setMentionQuery(null);
    }
  }, [onChange]);

  const insertMention = useCallback((name: string) => {
    const before = value.slice(0, mentionStart);
    const after = value.slice(inputRef.current?.selectionStart ?? value.length);
    const newValue = `${before}@${name} ${after}`;
    onChange(newValue);
    setMentionQuery(null);
    setMentionStart(-1);
    setTimeout(() => {
      const pos = before.length + name.length + 2; // @name + space
      inputRef.current?.setSelectionRange(pos, pos);
      inputRef.current?.focus();
    }, 0);
  }, [value, mentionStart, onChange]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLInputElement>) => {
    if (mentionQuery !== null && filtered.length > 0) {
      if (e.key === "ArrowDown") { e.preventDefault(); setSelectedIdx(i => Math.min(i + 1, filtered.length - 1)); return; }
      if (e.key === "ArrowUp")   { e.preventDefault(); setSelectedIdx(i => Math.max(i - 1, 0)); return; }
      if (e.key === "Enter" || e.key === "Tab") { e.preventDefault(); insertMention(filtered[selectedIdx].name); return; }
      if (e.key === "Escape")    { setMentionQuery(null); return; }
    }
    if (e.key === "Enter" && !e.shiftKey && mentionQuery === null) {
      e.preventDefault();
      onSubmit();
    }
  }, [mentionQuery, filtered, selectedIdx, insertMention, onSubmit]);

  // Close on outside click
  useEffect(() => {
    if (mentionQuery === null) return;
    const handler = (e: MouseEvent) => {
      if (!inputRef.current?.parentElement?.contains(e.target as Node)) {
        setMentionQuery(null);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [mentionQuery]);

  return (
    <div className="relative flex-1">
      <input
        ref={inputRef}
        type="text"
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        className={cn(
          "w-full text-xs bg-transparent border-none outline-none focus-visible:ring-0 shadow-none",
          className
        )}
      />

      {/* Mention dropdown */}
      {mentionQuery !== null && filtered.length > 0 && (
        <div className="absolute bottom-full mb-1 left-0 w-52 bg-popover border border-border/50 rounded-md shadow-xl overflow-hidden z-50">
          {filtered.map((m, i) => {
            const initials = m.name.split(" ").map((w: string) => w[0]).join("").slice(0, 2).toUpperCase();
            return (
              <button
                key={m.id}
                type="button"
                onMouseDown={(e) => { e.preventDefault(); insertMention(m.name); }}
                className={cn(
                  "w-full flex items-center gap-2 px-2.5 py-1.5 text-left transition-colors",
                  i === selectedIdx ? "bg-primary/10 text-foreground" : "hover:bg-muted/40 text-foreground/80"
                )}
              >
                <Avatar className="h-5 w-5 shrink-0">
                  {m.avatarUrl && <AvatarImage src={m.avatarUrl} />}
                  <AvatarFallback className="text-[8px] bg-primary/10 text-primary font-black">{initials}</AvatarFallback>
                </Avatar>
                <span className="text-[11px] font-medium truncate">{m.name}</span>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
