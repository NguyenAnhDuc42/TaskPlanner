import { useState } from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "./create-task-form";

interface CreateTaskButtonProps {
  workspaceId: string;
  layerId: string;
  layerType: string;
}

export function CreateTaskButton({
  workspaceId,
  layerId,
  layerType,
}: CreateTaskButtonProps) {
  const [open, setOpen] = useState(false);

  return (
    <DialogFormWrapper
      open={open}
      onOpenChange={setOpen}
      title="Create Task"
      contentClassName="sm:max-w-[460px]"
      trigger={
        <Button variant="ghost" size="sm" className="h-7 text-xs gap-1.5 px-2">
          <Plus className="h-3 w-3" />
          Add Task
        </Button>
      }
    >
      <CreateTaskForm
        workspaceId={workspaceId}
        layerId={layerId}
        layerType={layerType}
        onSuccess={() => setOpen(false)}
      />
    </DialogFormWrapper>
  );
}
