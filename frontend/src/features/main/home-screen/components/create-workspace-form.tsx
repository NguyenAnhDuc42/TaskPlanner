import * as React from "react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { AttributeButton, IconColorPicker } from "@/features/workspace/components/forms/form-elements";
import { Lock, Globe } from "lucide-react";

export type FormData = {
  name: string;
  description: string;
  color: string;
  icon: string;
  strictJoin: boolean;
};

type Props = {
  onSubmit?: (data: FormData) => void;
  isLoading?: boolean;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
};

export function CreateWorkspaceForm({
  onSubmit,
  isLoading,
  open,
  onOpenChange,
}: Props) {
  const [data, setData] = React.useState<FormData>({
    name: "",
    description: "",
    color: "#4f46e5",
    icon: "AppWindow",
    strictJoin: false,
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!data.name.trim() || isLoading) return;

    onSubmit?.(data);

    // Reset state
    setData({
      name: "",
      description: "",
      color: "#4f46e5",
      icon: "AppWindow",
      strictJoin: false,
    });
    onOpenChange?.(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md p-0 overflow-hidden bg-background border border-border/40 shadow-2xl rounded-md">
        {/* Added Header */}
        <DialogHeader className="p-4 border-b border-border/40">
          <DialogTitle className="text-sm font-bold">Create Workspace</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex flex-col w-full p-4 gap-4">
          {/* Icon and Name on the same line with distinct boundary */}
          <div className="flex items-center gap-3 bg-muted/20 p-3 rounded-md border border-border/10">
            <IconColorPicker
              icon={data.icon}
              color={data.color}
              onChange={(i, c) => setData({ ...data, icon: i, color: c })}
            />
            <input
              placeholder="Workspace name"
              value={data.name}
              onChange={(e) => setData({ ...data, name: e.target.value })}
              className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none"
              autoFocus
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.shiftKey) {
                  e.preventDefault();
                  handleSubmit(e);
                }
              }}
            />
          </div>

          {/* Description Underneath with distinct boundary */}
          <div className="bg-muted/20 p-3 rounded-md border border-border/10">
            <textarea
              placeholder="Description (Optional)"
              value={data.description}
              onChange={(e) => setData({ ...data, description: e.target.value })}
              className="w-full bg-transparent border-none focus:ring-0 text-xs text-muted-foreground placeholder:text-muted-foreground/30 py-0 outline-none resize-none min-h-[60px]"
              rows={3}
            />
          </div>

          {/* Attribute Strip separated at the bottom */}
          <div className="flex items-center justify-between border-t border-border/10 pt-3">
            <AttributeButton
              icon={data.strictJoin ? Lock : Globe}
              active={data.strictJoin}
              onClick={() => setData({ ...data, strictJoin: !data.strictJoin })}
            >
              {data.strictJoin ? "Strict Join" : "Anyone can join"}
            </AttributeButton>

            <Button
              type="submit"
              disabled={!data.name.trim() || isLoading}
              className="h-8 px-4 bg-primary hover:bg-primary/90 text-primary-foreground text-xs font-bold rounded-md"
            >
              {isLoading ? "Creating..." : "Create Workspace"}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
