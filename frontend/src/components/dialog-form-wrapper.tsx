import { useState, type ReactNode } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "./ui/dialog";
import { cn } from "@/lib/utils";

interface Props {
  trigger: ReactNode;
  title: string;
  children: ReactNode;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  contentClassName?: string;
  hideHeader?: boolean;
}

export function DialogFormWrapper({
  trigger,
  title,
  children,
  open: controlledOpen,
  onOpenChange: setControlledOpen,
  contentClassName,
  hideHeader = false,
}: Props) {
  const [internalOpen, setInternalOpen] = useState(false);

  const isControlled = controlledOpen !== undefined;
  const open = isControlled ? controlledOpen : internalOpen;

  const handleOpenChange = (newOpen: boolean) => {
    if (!isControlled) setInternalOpen(newOpen);
    setControlledOpen?.(newOpen);
  };

  return (
    <>
      {isControlled ? trigger : (
        <div className="contents" onClick={() => handleOpenChange(true)}>{trigger}</div>
      )}

      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent className={cn("sm:max-w-[440px] w-full p-0 overflow-hidden border border-border/50 shadow-2xl rounded-xl bg-background outline-none", contentClassName)}>
          {!hideHeader && (
            <DialogHeader className="px-4 py-2.5 border-b border-border/5">
              <DialogTitle className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground/40">{title}</DialogTitle>
            </DialogHeader>
          )}
          <div className="w-full">
            {children}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
