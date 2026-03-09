"use client";

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
import { cn } from "@/lib/utils";
import {
  CalendarIcon,
  ChevronLeft,
  ChevronRight,
  LayoutGrid,
} from "lucide-react";

export function CalenderTasks() {
  const [date, setDate] = React.useState<Date | undefined>(new Date());
  const [isDialogOpen, setIsDialogOpen] = React.useState(false);

  const handleDateSelect = (selectedDate: Date | undefined) => {
    if (selectedDate) {
      setDate(selectedDate);
      setIsDialogOpen(true);
    }
  };

  return (
    <div className="flex flex-col h-full bg-[#0a0a0a]/80 backdrop-blur-xl border border-white/5 rounded-2xl overflow-hidden shadow-[0_0_50px_-12px_rgba(0,0,0,0.5)] group/container transition-all duration-700 hover:border-primary/20">
      {/* Dynamic Header Section */}
      <div className="relative p-6 flex items-start justify-between overflow-hidden">
        {/* Abstract background glow */}
        <div className="absolute -top-10 -right-10 w-32 h-32 bg-primary/5 blur-[60px] rounded-full group-hover/container:bg-primary/10 transition-colors duration-700" />

        <div className="relative z-10 flex flex-col items-start gap-1">
          <div className="flex items-center gap-2 px-2 py-0.5 bg-primary/10 border border-primary/20 rounded-full">
            <span className="text-[9px] font-mono font-black uppercase tracking-[0.25em] text-primary">
              Schedule
            </span>
          </div>
          <div className="flex flex-col mt-2">
            <h2 className="text-4xl font-mono font-black text-white tracking-tighter">
              {format(new Date(), "dd")}
            </h2>
            <p className="text-[10px] font-mono font-bold text-muted-foreground/60 uppercase tracking-widest leading-none">
              {format(new Date(), "MMMM yyyy")}
            </p>
          </div>
        </div>

        <div className="relative z-10 h-10 w-10 flex items-center justify-center rounded-xl bg-white/5 border border-white/10 group-hover/container:border-primary/30 transition-all duration-500 shadow-xl">
          <LayoutGrid className="h-5 w-5 text-white/40 group-hover/container:text-primary transition-colors" />
        </div>
      </div>

      {/* Modernized Calendar Body */}
      <div className="flex-1 px-4 pb-4 select-none">
        <Calendar
          mode="single"
          selected={date}
          onSelect={handleDateSelect}
          showOutsideDays={false}
          className="p-0 pointer-events-auto"
          classNames={{
            months: "w-full",
            month: "w-full space-y-4",
            caption: "hidden", // We use our own header
            table: "w-full border-collapse",
            head_row: "flex justify-between mb-4 px-2",
            head_cell:
              "text-muted-foreground/30 w-9 font-mono font-black text-[9px] uppercase tracking-tighter text-center",
            row: "flex w-full mt-1 justify-between",
            cell: "relative p-0 text-center text-sm focus-within:relative focus-within:z-20",
            day: cn(
              "h-9 w-9 p-0 font-mono font-bold text-[11px] rounded-xl transition-all duration-500 hover:bg-white/5 hover:text-white flex items-center justify-center relative group/day border border-transparent",
            ),
            day_selected:
              "bg-primary text-primary-foreground font-black scale-110 shadow-[0_0_20px_rgba(var(--primary),0.3)] hover:bg-primary hover:text-primary-foreground border-primary/50",
            day_today:
              "text-primary before:content-[''] before:absolute before:bottom-1 before:w-1 before:h-1 before:bg-primary before:rounded-full",
            day_outside:
              "text-muted-foreground/5 opacity-0 pointer-events-none",
            day_disabled: "text-muted-foreground/20 opacity-50",
            day_hidden: "invisible",
          }}
        />
      </div>

      {/* Decorative Interactive Footer */}
      <div className="px-6 py-4 bg-white/[0.02] border-t border-white/5 flex items-center justify-between group/footer cursor-pointer hover:bg-white/[0.04] transition-colors duration-500">
        <div className="flex items-center gap-2">
          <div className="w-1.5 h-1.5 rounded-full bg-primary animate-pulse" />
          <span className="text-[9px] font-mono font-black uppercase tracking-[0.2em] text-muted-foreground/40 group-hover/footer:text-primary transition-colors">
            Synchronized
          </span>
        </div>
        <ChevronRight className="h-3 w-3 text-muted-foreground/20 group-hover/footer:text-white transition-all transform group-hover/footer:translate-x-1" />
      </div>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="max-w-md border-white/10 bg-[#0a0a0a]/95 backdrop-blur-2xl shadow-[0_0_100px_rgba(0,0,0,1)] p-0 overflow-hidden rounded-3xl">
          <div className="relative p-8">
            <div className="absolute top-0 right-0 w-32 h-32 bg-primary/10 blur-[60px] rounded-full" />

            <DialogHeader className="relative z-10 space-y-4">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 flex items-center justify-center rounded-xl bg-primary/20 border border-primary/30">
                  <CalendarIcon className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <DialogTitle className="font-mono text-2xl font-black text-white tracking-tighter leading-none">
                    {date ? format(date, "MMM dd") : "Schedule"}
                  </DialogTitle>
                  <p className="text-[10px] font-mono font-bold text-muted-foreground/40 uppercase tracking-[0.2em] mt-1">
                    Event Registry
                  </p>
                </div>
              </div>
              <DialogDescription className="hidden" />
            </DialogHeader>

            <div className="mt-8 relative z-10">
              <div className="py-16 flex flex-col items-center justify-center border border-white/5 rounded-2xl bg-white/[0.02] relative group/dialog-box overflow-hidden">
                <div className="absolute inset-0 bg-gradient-to-br from-primary/5 to-transparent opacity-0 group-hover/dialog-box:opacity-100 transition-opacity duration-1000" />
                <p className="text-muted-foreground/30 text-[10px] font-mono font-black uppercase tracking-[0.3em] text-center px-12 leading-relaxed">
                  No specific operations identified for this temporal node.
                </p>
              </div>
            </div>

            <div className="mt-6 flex justify-end">
              <button
                onClick={() => setIsDialogOpen(false)}
                className="px-6 py-2 bg-white/5 hover:bg-white/10 text-white font-mono font-bold text-[10px] uppercase tracking-widest rounded-lg border border-white/10 transition-all active:scale-95"
              >
                Acknowledge
              </button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
