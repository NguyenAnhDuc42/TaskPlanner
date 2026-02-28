import { createFileRoute } from "@tanstack/react-router";
import MembersIndex from "@/features/workspace/contents/members/members-index";

export const Route = createFileRoute("/workspaces/$workspaceId/members")({
  component: MembersIndex,
});
