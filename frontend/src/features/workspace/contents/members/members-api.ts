import { workspaceApi } from "@/store/workspaceApi";
import type { addMembersSchema, updateMembersSchema } from "./members-type";
import type { PagedResult } from "@/types/paged-result";
import { toast } from "sonner";
import type z from "zod";
import React from "react";
import { memberSlice } from "@/store/entityStore";
import type { MemberRecord } from "@/types/workspace/member-record";

export const membersApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getMembers: build.query<PagedResult<MemberRecord>, {
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
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(memberSlice.actions.upsertMany(data.items));
        } catch { /* ignore */ }
      },
      // Infinite scroll: reuse cache based only on filters
      serializeQueryArgs: ({ endpointName, queryArgs }) => {
        const filters = { ...queryArgs };
        delete filters.cursor;
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
    addMembers: build.mutation<void, { workspaceId: string; values: z.infer<typeof addMembersSchema> }>({
      query: ({ workspaceId, values }) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "POST",
        data: values,
      }),
      invalidatesTags: ["Members"],
    }),
    updateMembers: build.mutation<void, { workspaceId: string; values: z.infer<typeof updateMembersSchema> }>({
      query: ({ workspaceId, values }) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "PATCH",
        data: values,
      }),
      // SignalR upserts the updated members into the entity store — no refetch needed
    }),
    removeMembers: build.mutation<void, { workspaceId: string; memberIds: string[] }>({
      query: ({ workspaceId, memberIds }) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "DELETE",
        data: { memberIds },
      }),
      // SignalR removes deleted members from the entity store — selectById returns undefined
      // and the !!r filter in members-index drops them without a refetch
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

  const { data, isLoading, isFetching, isError } = useGetMembersQuery({ workspaceId, ...filters, cursor });

  const fetchNextPage = React.useCallback(() => {
    if (data?.nextCursor) {
      setCursor(data.nextCursor);
    }
  }, [data]);

  return {
    data: data ? { pages: [data] } : undefined,
    isLoading,
    isError,
    isFetchingNextPage: isFetching && cursor !== null,
    hasNextPage: !!data?.nextCursor,
    fetchNextPage,
  };
}

export function useAddMembers(workspaceId: string) {
  const [addTrigger, { isLoading }] = useAddMembersMutation();
  return {
    mutate: async (values: z.infer<typeof addMembersSchema>) => {
      try {
        const result = await addTrigger({ workspaceId, values }).unwrap();
        toast.success("Members added successfully");
        return result;
      } catch (error: unknown) {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to add members");
        throw error;
      }
    },
    isPending: isLoading,
  };
}

export function useUpdateMembers(workspaceId: string) {
  const [updateTrigger, { isLoading }] = useUpdateMembersMutation();
  return {
    mutate: async (values: z.infer<typeof updateMembersSchema>) => {
      try {
        const result = await updateTrigger({ workspaceId, values }).unwrap();
        toast.success("Members updated successfully");
        return result;
      } catch (error: unknown) {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to update members");
        throw error;
      }
    },
    isPending: isLoading,
  };
}

export function useRemoveMembers(workspaceId: string) {
  const [removeTrigger, { isLoading }] = useRemoveMembersMutation();
  return {
    mutate: async (memberIds: string[]) => {
      try {
        const result = await removeTrigger({ workspaceId, memberIds }).unwrap();
        toast.success("Members removed successfully");
        return result;
      } catch (error: unknown) {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to remove members");
        throw error;
      }
    },
    isPending: isLoading,
  };
}
