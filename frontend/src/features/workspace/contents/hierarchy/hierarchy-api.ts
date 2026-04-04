import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import type { WorkspaceHierarchy } from "./hierarchy-type";
import { hierarchyKeys } from "./hierarchy-keys";

const MOCK_HIERARCHY: WorkspaceHierarchy = {
  id: "w1",
  name: "Main Workspace",
  spaces: [
    {
      id: "mock-s1",
      name: "Engineering",
      color: "#3b82f6",
      icon: "Zap",
      isPrivate: false,
      folders: [
        {
          id: "mock-f1",
          name: "Frontend",
          color: "#60a5fa",
          icon: "Monitor",
          isPrivate: false,
          tasks: [
            { id: "mock-t1", name: "Design System Implementation", priority: 1, icon: "Palette" },
            { id: "mock-t2", name: "Sidebar Refactor", priority: 2, icon: "Layout" },
          ],
        },
        {
          id: "mock-f2",
          name: "Backend",
          color: "#2563eb",
          icon: "Server",
          isPrivate: true,
          tasks: [
            { id: "mock-t3", name: "API Optimization", priority: 3, icon: "Zap" },
            { id: "mock-t4", name: "Database Schema Update", priority: 2, icon: "Database" },
          ],
        }
      ],
      tasks: [
        { id: "mock-t5", name: "Weekly Sync", priority: 1, icon: "Clock" },
        { id: "mock-t6", name: "Onboarding Docs", priority: 3, icon: "FileText" },
      ],
    },
    {
      id: "mock-s2",
      name: "Product & Design",
      color: "#8b5cf6",
      icon: "Palette",
      isPrivate: false,
      folders: [
        {
          id: "mock-f3",
          name: "Q2 Roadmap",
          color: "#a78bfa",
          icon: "Calendar",
          isPrivate: false,
          tasks: [
            { id: "mock-t7", name: "Stakeholder Interviews", priority: 2, icon: "Users" },
          ],
        }
      ],
      tasks: [
        { id: "mock-t8", name: "Brainstorming Session", priority: 1, icon: "Lightbulb" },
      ],
    },
    {
      id: "mock-s3",
      name: "Marketing",
      color: "#ec4899",
      icon: "Megaphone",
      isPrivate: true,
      folders: [],
      tasks: [
        { id: "mock-t9", name: "Social Media Campaign", priority: 1, icon: "Share2" },
      ],
    }
  ]
};

export function useHierarchy(workspaceId: string) {
  return useQuery({
    queryKey: hierarchyKeys.detail(workspaceId),
    queryFn: async () => {
      // In a real app, we'd fetch from API. For now, we use the rich mock.
      return MOCK_HIERARCHY;
    },
    staleTime: 1000 * 60 * 5,
  });
}

// ... (Hooks for mutations remain the same)

export function useEntityInfo(workspaceId: string, entityId: string | undefined) {
  const { data: hierarchy } = useHierarchy(workspaceId);

  return useMemo(() => {
    if (!entityId || !hierarchy) return null;

    // Search Spaces
    const space = hierarchy.spaces.find((s) => s.id === entityId);
    if (space) return { id: space.id, name: space.name, icon: space.icon, color: space.color, type: "space" };

    // Search Folders
    for (const s of hierarchy.spaces) {
      const folder = s.folders.find((f) => f.id === entityId);
      if (folder) return { id: folder.id, name: folder.name, icon: folder.icon, color: folder.color, type: "folder" };
    }

    // Search Tasks
    for (const s of hierarchy.spaces) {
      const taskInSpace = s.tasks.find((t) => t.id === entityId);
      if (taskInSpace) return { id: taskInSpace.id, name: taskInSpace.name, icon: taskInSpace.icon || "CheckSquare", color: taskInSpace.color || s.color, type: "task" };
      
      for (const f of s.folders) {
        const taskInFolder = f.tasks.find((t) => t.id === entityId);
        if (taskInFolder) return { id: taskInFolder.id, name: taskInFolder.name, icon: taskInFolder.icon || "CheckSquare", color: taskInFolder.color || f.color, type: "task" };
      }
    }

    return null;
  }, [hierarchy, entityId]);
}
