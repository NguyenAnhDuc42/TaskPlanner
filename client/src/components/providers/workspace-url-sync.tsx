"use client"

import { useWorkspaceStore } from "@/utils/workspace-store";
import { useParams, usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";

export default function WorkspaceUrlSync() {
  const params = useParams();
  const pathname = usePathname();
  const router = useRouter(); // Add router
  const { selectedWorkspaceId, setSelectedWorkspaceId } = useWorkspaceStore();
  const urlWorkspaceId = params.workspaceId as string | undefined;

  useEffect(() => {
    const isWorkspaceRoute = pathname.startsWith("/ws");
    
    // Handle invalid workspace routes
    if (isWorkspaceRoute && !urlWorkspaceId && selectedWorkspaceId) {
      // Redirect to valid workspace URL
      router.replace(`/ws/${selectedWorkspaceId}`);
      return;
    }

    if (urlWorkspaceId) {
      if (urlWorkspaceId !== selectedWorkspaceId) {
        setSelectedWorkspaceId(urlWorkspaceId);
      }
    } else if (isWorkspaceRoute) {
      setSelectedWorkspaceId(undefined);
    }
  }, [urlWorkspaceId, selectedWorkspaceId, setSelectedWorkspaceId, pathname, router]);

  return null;
}