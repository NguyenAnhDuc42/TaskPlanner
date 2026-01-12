
import { Button } from "@/components/ui/button";
import { UserPlus } from "lucide-react";

export function MembersSidebar() {
  return (
    <div className="space-y-4 animate-in fade-in slide-in-from-left-1 duration-200">
      <div className="px-1 py-2">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
          Workspace Team
        </h3>
        <div className="space-y-1">
          <Button
            variant="ghost"
            className="w-full justify-start gap-3 h-10 px-3 hover:bg-accent/50 group"
          >
            <div className="h-6 w-6 rounded-full bg-primary/20 flex items-center justify-center text-[10px] font-bold group-hover:bg-primary/30 transition-colors">
              JD
            </div>
            <span className="text-sm font-medium">John Doe</span>
          </Button>
          <Button
            variant="ghost"
            className="w-full justify-start gap-3 h-10 px-3 hover:bg-accent/50 group"
          >
            <div className="h-6 w-6 rounded-full bg-blue-500/20 flex items-center justify-center text-[10px] font-bold group-hover:bg-blue-500/30 transition-colors">
              AS
            </div>
            <span className="text-sm font-medium">Alice Smith</span>
          </Button>
        </div>
      </div>

      <Button
        variant="outline"
        className="w-full gap-2 border-dashed h-10 hover:border-primary/50 hover:bg-primary/5 transition-all"
      >
        <UserPlus className="h-4 w-4" />
        <span className="text-sm font-medium">Invite Member</span>
      </Button>
    </div>
  );
}
