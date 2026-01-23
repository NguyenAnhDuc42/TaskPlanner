import {
  infiniteQueryOptions,
  keepPreviousData,
  useInfiniteQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import type {
  addMembersSchema,
  MemberSummary,
  updateMembersSchema,
} from "./members-type";
import type { PagedResult } from "@/types/paged-result";
import { api } from "@/lib/api-client";
import { membersKeys } from "./members-key";
import type z from "zod";
import { toast } from "sonner";
import { isAxiosError } from "axios";

export const membersInfiniteQueryOptions = (
  workspaceId: string,
  filters: {
    name?: string;
    email?: string;
    role?: string;
  } = {},
) =>
  infiniteQueryOptions({
    queryKey: [...membersKeys.list(workspaceId), filters],
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

export function useMembers(
  workspaceId: string,
  filters?: {
    name?: string;
    email?: string;
    role?: string;
  },
) {
  return useInfiniteQuery({
    ...membersInfiniteQueryOptions(workspaceId, filters),
    staleTime: 1000 * 60 * 2,
    refetchOnWindowFocus: false,
    placeholderData: keepPreviousData,
  });
}

type AddMembersValues = z.infer<typeof addMembersSchema>;
export function useAddMembers(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (values: AddMembersValues) => {
      const result = await api.post(`/workspaces/${workspaceId}/members`, {
        ...values,
      });
      return result.data;
    },
    onSuccess: async () => {
      toast.success("Members added successfully");
      await queryClient.invalidateQueries({
        queryKey: membersKeys.list(workspaceId),
      });
    },
    onError: (error) => {
      if (isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        const message = data.detail || data.title || "Failed to add members";
        toast.error(message);
      } else {
        toast.error("An unexpected error occurred");
      }
    },
  });
}

type UpdateMembersValues = z.infer<typeof updateMembersSchema>;
export function useUpdateMembers(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (values: UpdateMembersValues) => {
      const result = await api.patch(`/workspaces/${workspaceId}/members`, {
        ...values,
      });
      return result.data;
    },
    onSuccess: async () => {
      toast.success("Members updated successfully");
      await queryClient.invalidateQueries({
        queryKey: membersKeys.list(workspaceId),
      });
    },
    onError: (error) => {
      if (isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        const message = data.detail || data.title || "Failed to update members";
        toast.error(message);
      } else {
        toast.error("An unexpected error occurred");
      }
    },
  });
}

export function useRemoveMembers(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (memberIds: string[]) => {
      const result = await api.delete(`/workspaces/${workspaceId}/members`, {
        data: { memberIds },
      });
      return result.data;
    },
    onSuccess: async () => {
      toast.success("Members removed successfully");
      await queryClient.invalidateQueries({
        queryKey: membersKeys.list(workspaceId),
      });
    },
    onError: (error) => {
      if (isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        const message = data.detail || data.title || "Failed to remove members";
        toast.error(message);
      } else {
        toast.error("An unexpected error occurred");
      }
    },
  });
}
