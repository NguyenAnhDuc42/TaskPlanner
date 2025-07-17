"use client"

import { useWorkspaceStore } from "@/utils/workspace-store";
import { useParams } from "next/navigation";
import { useEffect } from "react";

export default function WorkspaceUrlSync() {
  const params = useParams();
  const { selectedWorkspaceId, setSelectedWorkspaceId } = useWorkspaceStore();
  const urlWorkspaceId = params.workspaceId as string | undefined;

  useEffect(() => {
    if(urlWorkspaceId && urlWorkspaceId !== selectedWorkspaceId){
        setSelectedWorkspaceId(urlWorkspaceId);
    }
  }, [urlWorkspaceId, selectedWorkspaceId, setSelectedWorkspaceId]);

  return null;
}
