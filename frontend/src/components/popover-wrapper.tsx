import { useState, type ReactNode } from "react";
import { Popover, PopoverContent, PopoverTrigger } from "./ui/popover";

interface Props {
  trigger: ReactNode; // Your button component
  children: ReactNode; // Your form component
  onOpenChange?: (open: boolean) => void;
}

export function PopoverFormWrapper({trigger,children,onOpenChange,}: Props) {
  const [open, setOpen] = useState(false);

  const handleOpenChange = (newOpen: boolean) => {
    setOpen(newOpen);
    onOpenChange?.(newOpen);
  };

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger asChild>{trigger}</PopoverTrigger>
      <PopoverContent className="w-80">{children}</PopoverContent>
    </Popover>
  );
}