import { useQuery } from "@tanstack/react-query";
import { commandCenterKeys } from "./command-center-key";
import type { CommandCenterStats } from "./command-center-type";

export function useCommandCenterStats(workspaceId: string) {
  return useQuery({
    queryKey: commandCenterKeys.stats(workspaceId),
    queryFn: async () => {
      // For now, returning mock data until endpoint is ready
      // const { data } = await api.get<CommandCenterStats>(`/workspaces/${workspaceId}/stats`);
      // return data;
      return {
        activeProjects: 12,
        pendingTasks: 48,
        velocity: 2.4,
        health: "98%",
      } as CommandCenterStats;
    },
    enabled: !!workspaceId,
  });
}
