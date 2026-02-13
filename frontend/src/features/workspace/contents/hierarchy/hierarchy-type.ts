export interface ListHierarchy {
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
}

export interface FolderHierarchy {
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
  lists: ListHierarchy[];
}

export interface SpaceHierarchy {
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
  folders: FolderHierarchy[];
  lists: ListHierarchy[];
}

export interface WorkspaceHierarchy {
  id: string;
  name: string;
  spaces: SpaceHierarchy[];
}

import { z } from "zod";

export const createSpaceSchema = z.object({
  name: z.string().min(1, "Name is required"),
  description: z.string().optional(),
  color: z.string().optional(),
  icon: z.string().optional(),
  isPrivate: z.boolean().optional(),
});
export type CreateSpaceRequest = z.infer<typeof createSpaceSchema>;

export const createFolderSchema = z.object({
  spaceId: z.string().uuid(),
  name: z.string().min(1, "Name is required"),
  color: z.string().optional(),
  icon: z.string().optional(),
  isPrivate: z.boolean().optional(),
});
export type CreateFolderRequest = z.infer<typeof createFolderSchema>;

export const createListSchema = z
  .object({
    spaceId: z.string().uuid().optional(),
    folderId: z.string().uuid().optional(),
    name: z.string().min(1, "Name is required"),
    color: z.string().optional(),
    icon: z.string().optional(),
    isPrivate: z.boolean().optional(),
  })
  .refine((data) => data.spaceId || data.folderId, {
    message: "Either spaceId or folderId must be provided",
  });
export type CreateListRequest = z.infer<typeof createListSchema>;

export const updateSpaceSchema = z.object({
  spaceId: z.string().uuid(),
  name: z.string().min(1).optional(),
  description: z.string().optional(),
  color: z.string().optional(),
  icon: z.string().optional(),
  isPrivate: z.boolean().optional(),
  memberIdsToAdd: z.array(z.string().uuid()).optional(),
});
export type UpdateSpaceRequest = z.infer<typeof updateSpaceSchema>;

export const updateFolderSchema = z.object({
  folderId: z.string().uuid(),
  name: z.string().min(1).optional(),
  color: z.string().optional(),
  icon: z.string().optional(),
  isPrivate: z.boolean().optional(),
  memberIdsToAdd: z.array(z.string().uuid()).optional(),
});
export type UpdateFolderRequest = z.infer<typeof updateFolderSchema>;

export const updateListSchema = z.object({
  listId: z.string().uuid(),
  name: z.string().min(1).optional(),
  color: z.string().optional(),
  icon: z.string().optional(),
  isPrivate: z.boolean().optional(),
  startDate: z.string().datetime().optional(),
  dueDate: z.string().datetime().optional(),
  memberIdsToAdd: z.array(z.string().uuid()).optional(),
});
export type UpdateListRequest = z.infer<typeof updateListSchema>;
