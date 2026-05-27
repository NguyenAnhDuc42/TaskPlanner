import { workspaceApi } from "@/store/workspaceApi";
import type { MemberSummary, addMembersSchema, updateMembersSchema } from "./members-type";
import type { PagedResult } from "@/types/paged-result";
import { toast } from "sonner";
import type z from "zod";
import React from "react";

export const membersApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getMembers: build.query<PagedResult<MemberSummary>, {
      workspaceId: string;
      name?: string;
      email?: string;
      role?: string;
      cursor?: string | null;
    }>({
      query: ({ workspaceId, ...params }) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "GET",
        params,
      }),
      // Infinite scroll: reuse cache based only on filters
      serializeQueryArgs: ({ endpointName, queryArgs }) => {
        const { cursor, ...filters } = queryArgs;
        return `${endpointName}_${JSON.stringify(filters)}`;
      },
      merge: (currentCache, newItems, { arg }) => {
        if (!arg.cursor) {
          return newItems;
        }
        currentCache.items.push(...newItems.items);
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
      },
      forceRefetch({ currentArg, previousArg }) {
        return currentArg !== previousArg;
      },
      providesTags: ["Members"],
    }),
    addMembers: build.mutation<any, { workspaceId: string; values: z.infer<typeof addMembersSchema> }>({
      query: ({ workspaceId, values }) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "POST",
        data: values,
      }),
      invalidatesTags: ["Members"],
    }),
    updateMembers: build.mutation<any, { workspaceId: string; values: z.infer<typeof updateMembersSchema> }>({
      query: ({ workspaceId, values }) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "PATCH",
        data: values,
      }),
      invalidatesTags: ["Members"],
    }),
    removeMembers: build.mutation<any, { workspaceId: string; memberIds: string[] }>({
      query: ({ workspaceId, memberIds }) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "DELETE",
        data: { memberIds },
      }),
      invalidatesTags: ["Members"],
    }),
  }),
});

export const {
  useGetMembersQuery,
  useAddMembersMutation,
  useUpdateMembersMutation,
  useRemoveMembersMutation,
} = membersApi;

export function useMembers(
  workspaceId: string,
  filters?: {
    name?: string;
    email?: string;
    role?: string;
  },
) {
  const [cursor, setCursor] = React.useState<string | null>(null);

  const prevFiltersRef = React.useRef(filters);
  React.useEffect(() => {
    if (JSON.stringify(prevFiltersRef.current) !== JSON.stringify(filters)) {
      setCursor(null);
      prevFiltersRef.current = filters;
    }
  }, [filters]);

  const { data, isLoading, isFetching } = useGetMembersQuery({ workspaceId, ...filters, cursor });

  const fetchNextPage = React.useCallback(() => {
    if (data?.nextCursor) {
      setCursor(data.nextCursor);
    }
  }, [data]);

  return {
    data: data ? { pages: [data] } : undefined,
    isLoading,
    isFetchingNextPage: isFetching && cursor !== null,
    hasNextPage: !!data?.nextCursor,
    fetchNextPage,
  };
}

export function useAddMembers(workspaceId: string) {
  const [addTrigger] = useAddMembersMutation();
  return {
    mutate: async (values: z.infer<typeof addMembersSchema>) => {
      try {
        const result = await addTrigger({ workspaceId, values }).unwrap();
        toast.success("Members added successfully");
        return result;
      } catch (error: any) {
        toast.error(error.message || "Failed to add members");
        throw error;
      }
    },
  };
}

export function useUpdateMembers(workspaceId: string) {
  const [updateTrigger] = useUpdateMembersMutation();
  return {
    mutate: async (values: z.infer<typeof updateMembersSchema>) => {
      try {
        const result = await updateTrigger({ workspaceId, values }).unwrap();
        toast.success("Members updated successfully");
        return result;
      } catch (error: any) {
        toast.error(error.message || "Failed to update members");
        throw error;
      }
    },
  };
}

export function useRemoveMembers(workspaceId: string) {
  const [removeTrigger] = useRemoveMembersMutation();
  return {
    mutate: async (memberIds: string[]) => {
      try {
        const result = await removeTrigger({ workspaceId, memberIds }).unwrap();
        toast.success("Members removed successfully");
        return result;
      } catch (error: any) {
        toast.error(error.message || "Failed to remove members");
        throw error;
      }
    },
  };
}
