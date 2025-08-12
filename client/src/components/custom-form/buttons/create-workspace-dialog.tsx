import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { useState, ReactElement, cloneElement } from "react";
import { CreateWorkspaceForm } from "../create/create-workspace-form";


interface CreateWorkspaceDialogProps {
  children: ReactElement<{ onClick?: (e: React.MouseEvent) => void }>;
}

export function CreateWorkspaceDialog({ children }: CreateWorkspaceDialogProps) { 
  const [open, setOpen] = useState(false);

  const handleCancel = () => { setOpen(false);};

  const handleSuccess = () => { setOpen(false);};

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
          className="max-w-md bg-black border-gray-800 text-white"
          onClick={(e) => e.stopPropagation()}
          onKeyDown={(e) => e.stopPropagation()}
          onFocus={(e) => e.stopPropagation()}>
        <DialogHeader>
          <DialogTitle className="text-white">Create New Workspace</DialogTitle>
          <DialogDescription className="text-gray-400">
            Set up a new workspace for your team to collaborate and organize projects.
          </DialogDescription>
        </DialogHeader>
        <CreateWorkspaceForm
          onCancel={handleCancel}
          onSuccess={handleSuccess} // Pass the new onSuccess handler
        />
      </DialogContent>
    </Dialog>
  );
}