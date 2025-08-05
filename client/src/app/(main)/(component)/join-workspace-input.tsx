import { useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useJoinWorkspace } from "@/features/user/user-hooks";

interface JoinWorkspaceInputProps {
  className?: string;
}

export function JoinWorkspaceInput({ className, }: JoinWorkspaceInputProps) {
  const [workspaceId, setWorkspaceId] = useState("");
  const { mutate: joinWorkspaceMutation, isPending: isJoiningWorkspace } = useJoinWorkspace();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (workspaceId.trim()) {
      joinWorkspaceMutation(workspaceId.trim());
      setWorkspaceId(""); // Clear input after submission
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className={cn("flex items-center gap-2", className)}
    >
      <Input
        type="text"
        placeholder="Enter workspace ID to join..."
        value={workspaceId}
        onChange={(e) => setWorkspaceId(e.target.value)}
        className="flex-1 bg-gray-900 border-gray-700 text-white placeholder-gray-400 focus:border-white focus:ring-white"
        disabled={isJoiningWorkspace}
      />
      <Button type="submit" disabled={isJoiningWorkspace} className="bg-white text-black hover:bg-gray-200">
        Join
      </Button>
    </form>
  );
}
