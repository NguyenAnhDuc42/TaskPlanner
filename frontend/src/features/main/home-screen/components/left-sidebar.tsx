import { Bell, Calendar as CalendarIcon } from "lucide-react";
import { Card } from "@/components/ui/card";
import { CalenderTasks } from "./calender-tasks";
import { NotificationsList } from "./notifications-list";

export function LeftSidebar() {
  return (
    <Card className="hidden xl:flex w-72 flex-shrink-0 flex flex-col h-full bg-card border border-border/50 shadow-sm overflow-hidden rounded-2xl">
      <div className="p-4 flex flex-col min-h-0 flex-1 gap-6">
        {/* Calendar Section */}
        <div className="flex flex-col gap-3">
          <div className="flex items-center gap-2 px-1">
            <CalendarIcon className="h-4 w-4 text-primary" />
            <h2 className="text-[10px] font-mono font-black uppercase tracking-[0.2em] text-foreground">
              Schedule
            </h2>
          </div>
          <div className="flex justify-center bg-muted/10 rounded-sm p-1 border border-border/40">
            <CalenderTasks />
          </div>
        </div>

        {/* Notifications Section */}
        <div className="flex flex-col min-h-0 flex-1">
          <div className="flex items-center gap-2 px-1 mb-3">
            <Bell className="h-4 w-4 text-primary" />
            <h2 className="text-[10px] font-mono font-black uppercase tracking-[0.2em] text-foreground">
              Notifications
            </h2>
          </div>
          <div className="flex-1 min-h-0">
            <NotificationsList />
          </div>
        </div>
      </div>
    </Card>
  );
}
