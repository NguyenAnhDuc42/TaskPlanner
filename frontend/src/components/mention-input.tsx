import { useState, useRef, useCallback, useEffect } from "react";
import { useSelector } from "react-redux";
import { memberSelectors } from "@/store/entityStore";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";
import type { MemberRecord } from "@/types/workspace";

interface MentionInputProps {
  value: string;
  onChange: (v: string) => void;
  onSubmit: () => void;
  placeholder?: string;
  className?: string;
}

// A mention record: what @Name appears in the display string and what ID it maps to
interface MentionEntry { displayName: string; memberId: string; }

export function MentionInput({ value, onChange, onSubmit, placeholder, className }: MentionInputProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [mentionQuery, setMentionQuery] = useState<string | null>(null);
  const [mentionStart, setMentionStart] = useState<number>(-1);
  const [selectedIdx, setSelectedIdx] = useState(0);

  // Track mention entries so we can serialize display text → @[id] tokens on submit
  const mentionMapRef = useRef<MentionEntry[]>([]);

  const allMembers = useSelector(memberSelectors.selectAll);
  const filtered = mentionQuery !== null
    ? allMembers.filter(m => m.name.toLowerCase().includes(mentionQuery.toLowerCase())).slice(0, 6)
    : [];

  // Serialize display text: replace @Name with @[memberId] for each tracked mention
  const serialize = useCallback((displayText: string): string => {
    let result = displayText;
    // Replace longest names first to avoid partial replacements
    const sorted = [...mentionMapRef.current].sort((a, b) => b.displayName.length - a.displayName.length);
    for (const entry of sorted) {
      result = result.replace(`@${entry.displayName}`, `@[${entry.memberId}]`);
    }
    return result;
  }, []);

  // Reset mention map when value is cleared from outside (after submit)
  useEffect(() => {
    if (value === "") mentionMapRef.current = [];
  }, [value]);

  const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const v = e.target.value;
    const cursor = e.target.selectionStart ?? v.length;
    const before = v.slice(0, cursor);
    const atIdx = before.lastIndexOf("@");

    if (atIdx !== -1) {
      const query = before.slice(atIdx + 1);
      if (!query.startsWith(" ")) {
        setMentionStart(atIdx);
        setMentionQuery(query);
        setSelectedIdx(0);
        // Update display value and serialize for parent
        onChange(serialize(v));
        return;
      }
    }
    setMentionQuery(null);
    onChange(serialize(v));
  }, [onChange, serialize]);

  const insertMention = useCallback((member: MemberRecord) => {
    const before = value.slice(0, mentionStart);
    const after = value.slice(inputRef.current?.selectionStart ?? value.length);
    const display = `${before}@${member.name} ${after}`;

    // Track this mention
    mentionMapRef.current = [...mentionMapRef.current, { displayName: member.name, memberId: member.id }];

    // Show display text to user, serialize for parent storage
    onChange(serialize(display));

    // Set input value to display text (controlled)
    if (inputRef.current) {
      inputRef.current.value = display;
    }

    setMentionQuery(null);
    setMentionStart(-1);
    setTimeout(() => {
      const pos = before.length + member.name.length + 2;
      inputRef.current?.setSelectionRange(pos, pos);
      inputRef.current?.focus();
    }, 0);
  }, [value, mentionStart, onChange, serialize]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLInputElement>) => {
    if (mentionQuery !== null && filtered.length > 0) {
      if (e.key === "ArrowDown") { e.preventDefault(); setSelectedIdx(i => Math.min(i + 1, filtered.length - 1)); return; }
      if (e.key === "ArrowUp")   { e.preventDefault(); setSelectedIdx(i => Math.max(i - 1, 0)); return; }
      if (e.key === "Enter" || e.key === "Tab") { e.preventDefault(); insertMention(filtered[selectedIdx]); return; }
      if (e.key === "Escape") { setMentionQuery(null); return; }
    }
    if (e.key === "Enter" && !e.shiftKey && mentionQuery === null) {
      e.preventDefault();
      onSubmit();
    }
  }, [mentionQuery, filtered, selectedIdx, insertMention, onSubmit]);

  // Derive display value: replace @[uuid] tokens back to @Name for display
  const displayValue = value.replace(/@\[([a-f0-9-]{36})\]/g, (_, id) => {
    const entry = mentionMapRef.current.find(e => e.memberId === id);
    if (entry) return `@${entry.displayName}`;
    // Fallback: look up from store
    const member = allMembers.find(m => m.id === id);
    return member ? `@${member.name}` : `@[${id.slice(0, 8)}]`;
  });

  useEffect(() => {
    if (mentionQuery === null) return;
    const handler = (e: MouseEvent) => {
      if (!inputRef.current?.parentElement?.contains(e.target as Node)) setMentionQuery(null);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [mentionQuery]);

  return (
    <div className="relative flex-1">
      <input
        ref={inputRef}
        type="text"
        value={displayValue}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        className={cn("w-full text-xs bg-transparent border-none outline-none focus-visible:ring-0 shadow-none", className)}
      />

      {mentionQuery !== null && filtered.length > 0 && (
        <div className="absolute bottom-full mb-1 left-0 w-52 bg-popover border border-border/50 rounded-md shadow-xl overflow-hidden z-50">
          {filtered.map((m, i) => {
            const initials = m.name.split(" ").map((w: string) => w[0]).join("").slice(0, 2).toUpperCase();
            return (
              <button
                key={m.id}
                type="button"
                onMouseDown={e => { e.preventDefault(); insertMention(m); }}
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
