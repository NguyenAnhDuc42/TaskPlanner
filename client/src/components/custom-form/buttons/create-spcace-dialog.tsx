
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { useState, ReactElement, cloneElement } from "react";
import { CreateSpaceForm } from "../create/create-space-form";


interface CreateSpaceDialogProps {
  workspaceId: string;
  children: ReactElement<{ onClick?: (e: React.MouseEvent) => void }>;
}

export function CreateSpaceDialog({ workspaceId, children }: CreateSpaceDialogProps) {
  const [open, setOpen] = useState(false);

  const handleCancel = () => {
    setOpen(false);
  };

  const handleSuccess = () => {
    setOpen(false);
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        {cloneElement(children, {
          onClick: (e: React.MouseEvent) => {
            e.preventDefault();
            setOpen(true);
            // Call original onClick if it exists
            if (children.props.onClick) {
              children.props.onClick(e);
            }
          }
        })}
      </DialogTrigger>
      <DialogContent
            className="sm:max-w-[400px] max-h-[90vh] overflow-y-auto bg-card border-border"
            onClick={(e) => e.stopPropagation()}
            onKeyDown={(e) => e.stopPropagation()}
            onFocus={(e) => e.stopPropagation()}>
        <DialogHeader>
          <DialogTitle className="text-foreground">Create a Space</DialogTitle>
          <DialogDescription className="text-muted-foreground">
            A Space represents teams, departments, or groups, each with its own Lists, workflows, and settings.
          </DialogDescription>
        </DialogHeader>
        <CreateSpaceForm
          workspaceId={workspaceId}
          onCancel={handleCancel}
          onSuccess={handleSuccess}
        />
      </DialogContent>
    </Dialog>
  );
}