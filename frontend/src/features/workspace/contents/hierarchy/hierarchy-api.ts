import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { api } from "@/lib/api-client";
import type {
  WorkspaceHierarchy,
  CreateSpaceRequest,
  CreateFolderRequest,
  CreateListRequest,
  UpdateSpaceRequest,
  UpdateFolderRequest,
  UpdateListRequest,
  EntityAccessMember,
} from "./hierarchy-type";
import { hierarchyKeys } from "./hierarchy-keys";

export function useHierarchy(workspaceId: string) {
  return useQuery({
    queryKey: hierarchyKeys.detail(workspaceId),
    queryFn: async () => {
      const { data } = await api.get<WorkspaceHierarchy>(
        `/workspaces/${workspaceId}/hierarchy`,
      );
      return data;
    },
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
}

export function useCreateFolder() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (data: CreateFolderRequest) => {
      await api.post(`/folders/space/${data.spaceId}`, {
        spaceId: data.spaceId,
        name: data.name,
        color: data.color || "#808080", // Default gray
        icon: data.icon || "folder", // Default folder icon
        isPrivate: data.isPrivate ?? false,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
    },
  });
}

export function useCreateList() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (data: CreateListRequest) => {
      const payload = {
        spaceId: data.spaceId,
        folderId: data.folderId,
        name: data.name,
        color: data.color || "#808080",
        icon: data.icon || "list",
        isPrivate: data.isPrivate ?? false,
      };

      if (data.folderId) {
        await api.post(`/lists/folder/${data.folderId}`, payload);
      } else if (data.spaceId) {
        await api.post(`/lists/space/${data.spaceId}`, payload);
      } else {
        throw new Error("Either spaceId or folderId must be provided");
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
    },
  });
}

export function useCreateSpace() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (data: CreateSpaceRequest) => {
      await api.post(`/spaces`, {
        workspaceId,
        name: data.name,
        color: data.color || "#808080",
        icon: data.icon || "space",
        isPrivate: data.isPrivate ?? false,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
    },
  });
}

export function useUpdateSpace() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (data: UpdateSpaceRequest) => {
      await api.put(`/spaces/${data.spaceId}`, data);
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.membersAccess("space", variables.spaceId),
      });
    },
  });
}

export function useDeleteSpace() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/spaces/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
    },
  });
}

export function useUpdateFolder() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (data: UpdateFolderRequest) => {
      await api.put(`/folders/${data.folderId}`, data);
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.membersAccess("folder", variables.folderId),
      });
    },
  });
}

export function useDeleteFolder() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/folders/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
    },
  });
}

export function useUpdateList() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (data: UpdateListRequest) => {
      await api.put(`/lists/${data.listId}`, data);
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.membersAccess("list", variables.listId),
      });
    },
  });
}

export function useSpaceMembersAccess(spaceId: string) {
  return useQuery({
    queryKey: hierarchyKeys.membersAccess("space", spaceId),
    queryFn: async () => {
      const { data } = await api.get<EntityAccessMember[]>(
        `/spaces/${spaceId}/members-access`,
      );
      return data;
    },
    enabled: !!spaceId,
  });
}

export function useFolderMembersAccess(folderId: string) {
  return useQuery({
    queryKey: hierarchyKeys.membersAccess("folder", folderId),
    queryFn: async () => {
      const { data } = await api.get<EntityAccessMember[]>(
        `/folders/${folderId}/members-access`,
      );
      return data;
    },
    enabled: !!folderId,
  });
}

export function useListMembersAccess(listId: string) {
  return useQuery({
    queryKey: hierarchyKeys.membersAccess("list", listId),
    queryFn: async () => {
      const { data } = await api.get<EntityAccessMember[]>(
        `/lists/${listId}/members-access`,
      );
      return data;
    },
    enabled: !!listId,
  });
}

export function useDeleteList() {
  const queryClient = useQueryClient();
  const { workspaceId } = useParams({ strict: false });

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/lists/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId || ""),
      });
    },
  });
}
