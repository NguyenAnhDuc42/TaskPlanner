import { createFileRoute } from "@tanstack/react-router";
import { ComingSoon } from "@/components/coming-soon";

export const Route = createFileRoute("/workspaces/$workspaceId/command-center")({
  component: () => (
    <ComingSoon 
      title="Command Center" 
      description="We are building a keyboard-first power hub. Soon you'll be able to quickly search, navigate, and run commands across your entire workspace." 
    />
  ),
});
