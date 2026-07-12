import { Loader2 } from "lucide-react";
import { UserAvatar } from "@/components/user-avatar";

export interface ChangeEntry {
  id: string;
  authorName: string;
  message: string;
  timestamp: string;
}

interface ChangesFeedProps {
  entries: ChangeEntry[];
  isLoading?: boolean;
}

export function ChangesFeed({ entries, isLoading }: Readonly<ChangesFeedProps>) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-3">
        <Loader2 className="h-3.5 w-3.5 animate-spin text-muted-foreground/40" />
      </div>
    );
  }

  if (entries.length === 0) {
    return (
      <p className="text-[10px] text-muted-foreground/40 italic py-2">No changes yet.</p>
    );
  }

  return (
    <div className="flex flex-col gap-2.5">
      {entries.map((entry) => (
        <div key={entry.id} className="flex items-start gap-2">
          <UserAvatar
            name={entry.authorName}
            className="h-4.5 w-4.5 rounded-sm shrink-0 mt-0.5"
            fallbackClassName="text-[7px] rounded-sm"
          />
          <div className="min-w-0 flex-1">
            <p className="text-[11px] text-foreground/80 leading-snug">
              <span className="font-semibold">{entry.authorName}</span> {entry.message}
            </p>
            <span className="text-[9px] text-muted-foreground/40">{entry.timestamp}</span>
          </div>
        </div>
      ))}
    </div>
  );
}
