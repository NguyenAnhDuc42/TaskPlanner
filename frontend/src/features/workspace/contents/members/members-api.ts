import {
  infiniteQueryOptions,
  keepPreviousData,
  useInfiniteQuery,
} from "@tanstack/react-query";
import type { MemberSummary } from "./members-type";
import type { PagedResult } from "@/types/paged-result";
import { api } from "@/lib/api-client";
import { membersKeys } from "./members-key";

export const membersInfiniteQueryOptions = (
  workspaceId: string,
  filters: {
    name?: string;
    email?: string;
    role?: string;
  } = {},
) =>
  infiniteQueryOptions({
    queryKey: [...membersKeys.list(), filters],
    queryFn: async ({ pageParam }: { pageParam: string | null }) => {
      const { data } = await api.get<PagedResult<MemberSummary>>(
        `/workspaces/${workspaceId}/members`,
        {
          params: {
            cursor: pageParam,
            ...filters,
          },
        },
      );
      return data;
    },
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor || undefined,
  });

export function useMembers(workspaceId: string, filters?: {
  name?: string;
  email?: string;
  role?: string;
}) {
  return useInfiniteQuery({
    ...membersInfiniteQueryOptions(workspaceId, filters),
    staleTime: 1000 * 60 * 2,
    refetchOnWindowFocus: false,
    placeholderData: keepPreviousData,
  });
}
