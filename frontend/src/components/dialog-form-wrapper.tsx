import { useState, type ReactNode } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "./ui/dialog";

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
        <DialogContent className={contentClassName}>
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
          </DialogHeader>
          {children}
        </DialogContent>
      </Dialog>
    </>
  );
}
