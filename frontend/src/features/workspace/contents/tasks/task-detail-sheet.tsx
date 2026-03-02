import React from "react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { Calendar, Flag, User, Clock, CheckCircle2 } from "lucide-react";
import type { TaskDto } from "./tasks-type";
import { format } from "date-fns";
import { Priority } from "@/types/priority";

interface TaskDetailSheetProps {
  task: TaskDto | null;
  isOpen: boolean;
  onClose: () => void;
}

export function TaskDetailSheet({
  task,
  isOpen,
  onClose,
}: TaskDetailSheetProps) {
  if (!task) return null;

  return (
    <Sheet open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="sm:max-w-xl w-[90%] p-0 bg-background/95 backdrop-blur-xl border-l border-primary/10">
        <div className="flex flex-col h-full">
          {/* Header Area */}
          <div className="p-6 pb-4 space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-muted-foreground" />
                <span className="text-xs font-bold text-muted-foreground uppercase tracking-widest">
                  Task Detail
                </span>
              </div>
              <Badge
                variant="secondary"
                className="bg-primary/5 text-primary border-primary/10 hover:bg-primary/10"
              >
                {task.statusId ? "In Progress" : "No Status"}
              </Badge>
            </div>

            <SheetHeader>
              <SheetTitle className="text-2xl font-black tracking-tight leading-tight">
                {task.name}
              </SheetTitle>
            </SheetHeader>
          </div>

          <Separator className="bg-primary/5" />

          {/* Attribution Section */}
          <div className="grid grid-cols-2 gap-px bg-primary/5">
            <AttributeItem
              icon={<Flag className="h-3.5 w-3.5" />}
              label="Priority"
              value={task.priority}
              valueColor={
                task.priority === Priority.Urgent
                  ? "text-red-500"
                  : "text-foreground"
              }
            />
            <AttributeItem
              icon={<User className="h-3.5 w-3.5" />}
              label="Assignee"
              value={task.assignees?.[0]?.name || "Unassigned"}
            />
            <AttributeItem
              icon={<Calendar className="h-3.5 w-3.5" />}
              label="Due Date"
              value={
                task.dueDate
                  ? format(new Date(task.dueDate), "MMM dd, yyyy")
                  : "No date"
              }
            />
            <AttributeItem
              icon={<Clock className="h-3.5 w-3.5" />}
              label="Story Points"
              value={task.storyPoints?.toString() || "0"}
            />
          </div>

          <Separator className="bg-primary/5" />

          {/* Description Section */}
          <div className="flex-1 p-6 space-y-4 overflow-y-auto">
            <div className="space-y-2">
              <h4 className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
                Description
              </h4>
              <div className="text-sm leading-relaxed text-foreground/80 min-h-[100px] whitespace-pre-wrap">
                {task.description ||
                  "No description provided. Click to add one..."}
              </div>
            </div>

            <div className="space-y-4 pt-4">
              <h4 className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
                Assignees
              </h4>
              <div className="flex flex-wrap gap-2">
                {task.assignees?.map((a) => (
                  <div
                    key={a.id}
                    className="flex items-center gap-2 bg-muted/50 px-3 py-1.5 rounded-full border border-primary/5"
                  >
                    <div className="h-5 w-5 rounded-full bg-primary/20 flex items-center justify-center text-[10px] font-bold">
                      {a.name.substring(0, 1)}
                    </div>
                    <span className="text-xs font-medium">{a.name}</span>
                  </div>
                ))}
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 w-8 rounded-full border border-dashed border-muted-foreground/20"
                >
                  <PlusIcon className="h-3 w-3" />
                </Button>
              </div>
            </div>
          </div>

          <div className="p-4 bg-muted/5 border-t border-primary/5 flex justify-end gap-2">
            <span className="text-[10px] text-muted-foreground font-medium italic">
              Created on {format(new Date(task.createdAt), "MMMM dd, yyyy")}
            </span>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  );
}

function AttributeItem({
  icon,
  label,
  value,
  valueColor = "text-foreground",
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  valueColor?: string;
}) {
  return (
    <div className="bg-background p-4 flex flex-col gap-1.5 hover:bg-muted/30 transition-colors cursor-pointer group">
      <div className="flex items-center gap-2 text-muted-foreground/60 group-hover:text-primary transition-colors">
        {icon}
        <span className="text-[10px] font-black uppercase tracking-widest">
          {label}
        </span>
      </div>
      <span className={`text-xs font-bold ${valueColor}`}>{value}</span>
    </div>
  );
}

function PlusIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      {...props}
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M5 12h14" />
      <path d="M12 5v14" />
    </svg>
  );
}
