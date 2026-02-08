import { useState, type ReactNode } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "./ui/dialog";

interface Props {
  trigger: ReactNode; // Your button component
  title: string;
  children: ReactNode; // Your form component
  onOpenChange?: (open: boolean) => void;
}

export function DialogFormWrapper({ trigger, title, children, onOpenChange }: Props) {
    const [open, setOpen] = useState(false);
    const handleOpenChange = (open: boolean) => {
        setOpen(open);
        onOpenChange?.(open);
    }
    
    return (
        <>
        <div onClick={() => handleOpenChange(true)}>{trigger}</div>
        
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{title}</DialogTitle>
                </DialogHeader>
                {children}
            </DialogContent>
        </Dialog>
        </>
    );
}