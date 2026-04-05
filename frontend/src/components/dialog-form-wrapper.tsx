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
}

export function DialogFormWrapper({
  trigger,
  title,
  children,
  open: controlledOpen,
  onOpenChange: setControlledOpen,
  contentClassName,
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
      <div onClick={() => handleOpenChange(true)}>{trigger}</div>

      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent className={cn("sm:max-w-[800px] w-full p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50", contentClassName)}>
          <DialogHeader className="px-6 py-4 border-b border-border/50">
            <DialogTitle className="text-sm font-black uppercase tracking-[0.2em] opacity-70">{title}</DialogTitle>
          </DialogHeader>
          <div className="w-full">
            {children}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
