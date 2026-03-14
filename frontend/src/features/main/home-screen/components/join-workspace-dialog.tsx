import * as React from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";

type Props = {
  onJoin: (code: string) => void;
  isLoading?: boolean;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export function JoinWorkspaceDialog({
  onJoin,
  isLoading,
  open,
  onOpenChange,
}: Props) {
  const [joinCode, setJoinCode] = React.useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!joinCode.trim() || isLoading) return;
    onJoin(joinCode.trim());
    setJoinCode("");
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md p-6">
        <DialogHeader className="pb-4">
          <DialogTitle>Join Workspace</DialogTitle>
          <DialogDescription>
            Enter the join code shared with you to access a workspace.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-6">
          <FieldGroup>
            <Field>
              <FieldLabel htmlFor="joinCode">Join Code</FieldLabel>
              <Input
                id="joinCode"
                placeholder="ABC-123-XYZ"
                value={joinCode}
                onChange={(e) => setJoinCode(e.target.value)}
                required
                disabled={isLoading}
                autoComplete="off"
                className="font-mono uppercase"
              />
            </Field>
          </FieldGroup>

          <Button
            type="submit"
            disabled={!joinCode.trim() || isLoading}
            className="w-full bg-primary hover:bg-primary/90 text-primary-foreground border-0 font-mono"
          >
            {isLoading ? "Joining..." : "Join Workspace"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}
