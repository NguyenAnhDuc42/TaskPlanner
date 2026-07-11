import { MessageSquare } from "lucide-react";

export function SpaceCommsPlaceholder() {
  return (
    <div className="flex-1 flex flex-col items-center justify-center gap-2 text-muted-foreground">
      <MessageSquare className="h-6 w-6 opacity-40" />
      <span className="text-xs font-medium">Communications — coming soon</span>
    </div>
  );
}
