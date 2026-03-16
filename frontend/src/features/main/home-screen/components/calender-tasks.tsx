
import * as React from "react";
import { Calendar } from "@/components/ui/calendar";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { format } from "date-fns";

export function CalenderTasks() {
  const [date, setDate] = React.useState<Date | undefined>(new Date());
  const [isDialogOpen, setIsDialogOpen] = React.useState(false);

  const handleDayClick = (day: Date) => {
    setDate(day);
    setIsDialogOpen(true);
  };

  const today = React.useMemo(() => new Date(), []);

  return (
    <div className="flex flex-col h-full p-2 bg-transparent border-0">

      <div className="flex-1 flex items-center justify-center overflow-hidden [--cell-size:30px]">
        <Calendar
          modifiers={{ selected: today }}
          onDayClick={handleDayClick}
          className="p-0 border-0 rounded-sm"
        />
      </div>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="font-mono text-lg font-bold">
              {date ? format(date, "MMMM do, yyyy") : "Schedule"}
            </DialogTitle>
            <DialogDescription className="font-mono text-xs">
              Upcoming tasks for this date.
            </DialogDescription>
          </DialogHeader>
          <div className="py-12 flex flex-col items-center justify-center border border-dashed border-border rounded-sm bg-muted/20">
            <p className="text-muted-foreground/60 text-[10px] font-mono uppercase tracking-widest text-center px-8">
              No tasks found for this day.
            </p>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
