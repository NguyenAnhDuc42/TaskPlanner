import { workspaceApi } from "@/store/workspaceApi";
import type { addMembersSchema, updateMembersSchema } from "./members-type";
import type { PagedResult } from "@/types/paged-result";
import { toast } from "sonner";
import type z from "zod";
import React from "react";
import { memberSlice, memberSelectors } from "@/store/entityStore";
import type { MemberRecord } from "@/types/workspace/member-record";
import type { RootState } from "@/store";

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
        } catch (error) {
          console.error("[membersApi] Failed to sync members to store:", error);
        }
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
      async onQueryStarted({ values }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const currentMembers = memberSelectors.selectEntities(state);
        
        const updates: MemberRecord[] = [];
        values.members.forEach((update) => {
          const existing = currentMembers[update.memberId];
          if (existing) {
            updates.push({
              ...existing,
              role: update.role ?? existing.role,
              status: update.status ?? existing.status,
            });
          }
        });

        if (updates.length > 0) {
          dispatch(memberSlice.actions.upsertMany(updates));
        }

        try {
          await queryFulfilled;
        } catch {
          // Revert on failure
          const reverts: MemberRecord[] = [];
          values.members.forEach((update) => {
            const existing = currentMembers[update.memberId];
            if (existing) reverts.push(existing);
          });
          if (reverts.length > 0) dispatch(memberSlice.actions.upsertMany(reverts));
        }
      },
    }),
    removeMembers: build.mutation<void, { workspaceId: string; memberIds: string[] }>({
      query: ({ workspaceId, memberIds }) => ({
        url: `/workspaces/${workspaceId}/members`,
        method: "DELETE",
        data: { memberIds },
      }),
      async onQueryStarted({ memberIds }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const currentMembers = memberSelectors.selectEntities(state);
        
        const reverts: MemberRecord[] = [];
        memberIds.forEach((id) => {
          const existing = currentMembers[id];
          if (existing) reverts.push(existing);
        });

        dispatch(memberSlice.actions.removeMany(memberIds));

        try {
          await queryFulfilled;
        } catch {
          if (reverts.length > 0) dispatch(memberSlice.actions.upsertMany(reverts));
        }
      },
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
