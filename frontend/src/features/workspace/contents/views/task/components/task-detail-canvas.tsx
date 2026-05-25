import * as React from "react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Calendar, CheckSquare, CircleDashed, Flag, MoreHorizontal, UserPlus, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";

interface TaskDetailCanvasProps {
  taskId?: string;
}

export function TaskDetailCanvas({ taskId }: TaskDetailCanvasProps) {
  return (
    <div className="flex flex-col h-full w-full bg-background overflow-hidden">


      {/* Task Content Scroll Area */}
      <div className="flex-1 overflow-y-auto">
        <div className="w-full p-6 md:p-10 space-y-8">
          
          {/* Title Area */}
          <div>
            <div className="flex items-start gap-3 mb-6">
              <CheckSquare className="h-7 w-7 text-muted-foreground/40 mt-1 shrink-0" />
              <h1 className="text-2xl md:text-3xl font-black text-foreground">
                Design the new landing page
              </h1>
            </div>
            
            {/* Properties Grid */}
            <div className="flex flex-wrap items-center gap-x-8 gap-y-4 text-sm text-muted-foreground pb-6 border-b border-border/30">
              
              {/* Status */}
              <div className="flex items-center gap-2">
                <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 w-16 shrink-0">Status</span>
                <Button variant="ghost" size="sm" className="h-6 px-2 text-xs font-medium bg-muted/20 hover:bg-muted/50 border border-border/50 rounded-sm">
                  <CircleDashed className="h-3.5 w-3.5 mr-1.5 text-muted-foreground" />
                  In Progress
                </Button>
              </div>

              {/* Priority */}
              <div className="flex items-center gap-2">
                <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 w-16 shrink-0">Priority</span>
                <Button variant="ghost" size="sm" className="h-6 px-2 text-xs font-medium text-destructive hover:text-destructive bg-destructive/10 hover:bg-destructive/20 border border-destructive/20 rounded-sm">
                  <Flag className="h-3.5 w-3.5 mr-1.5" />
                  High
                </Button>
              </div>

              {/* Assignee */}
              <div className="flex items-center gap-2">
                <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 w-16 shrink-0">Assignee</span>
                <div className="flex items-center gap-2 h-6">
                  <Avatar className="h-5 w-5">
                    <AvatarFallback className="text-[9px] bg-primary/20 text-primary">JD</AvatarFallback>
                  </Avatar>
                  <Button variant="ghost" size="sm" className="h-5 px-1.5 text-xs text-muted-foreground hover:text-foreground">
                    <UserPlus className="h-3 w-3 mr-1" /> Add
                  </Button>
                </div>
              </div>

              {/* Due Date */}
              <div className="flex items-center gap-2">
                <span className="font-mono text-[10px] uppercase tracking-wider opacity-50 w-16 shrink-0">Due Date</span>
                <Button variant="ghost" size="sm" className="h-6 px-2 text-xs font-medium text-foreground hover:bg-muted/50 rounded-sm">
                  <Calendar className="h-3.5 w-3.5 mr-1.5 text-muted-foreground" />
                  Aug 24, 2026
                </Button>
              </div>

            </div>
          </div>

          {/* Rich Text Editor Mock */}
          <div className="space-y-4">
            <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70 border-b border-border/50 pb-2">
              Description
            </h3>
            <div className="prose prose-sm dark:prose-invert max-w-none text-muted-foreground">
              <p>We need to redesign the main landing page to increase conversion rates. The current design is feeling a bit dated.</p>
              <ul>
                <li>Update hero section with new copy</li>
                <li>Add social proof testimonials</li>
                <li>Optimize CTA buttons for higher contrast</li>
              </ul>
              <p>Please refer to the Figma file attached in the comments.</p>
            </div>
          </div>
          
        </div>
      </div>
    </div>
  );
}
