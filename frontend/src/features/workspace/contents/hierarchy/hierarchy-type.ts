import {
  ASSIGNABLE_ACCESS_LEVELS,
  type AssignableAccessLevel,
} from "@/types/access-level";
import { z } from "zod";

export interface ListHierarchy {
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
  inheritStatus: boolean;
}

export interface FolderHierarchy {
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
  inheritStatus: boolean;
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

export interface EntityAccessMember {
  workspaceMemberId: string;
  userId: string;
  userName: string;
  userEmail: string;
  accessLevel: AccessLevel;
  createdAt: string;
  isCreator: boolean;
}

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

export const accessLevelSchema = z.enum(ASSIGNABLE_ACCESS_LEVELS);
export type AccessLevel = AssignableAccessLevel;

export const updateMemberValueSchema = z.object({
  workspaceMemberId: z.string().uuid(),
  accessLevel: accessLevelSchema.optional(),
  isRemove: z.boolean().optional(),
});
export type UpdateMemberValue = z.infer<typeof updateMemberValueSchema>;

export const updateSpaceSchema = z.object({
  spaceId: z.string().uuid(),
  name: z.string().min(1).optional(),
  description: z.string().optional(),
  color: z.string().optional(),
  icon: z.string().optional(),
  isPrivate: z.boolean().optional(),
});
export type UpdateSpaceRequest = z.infer<typeof updateSpaceSchema>;

export const updateFolderSchema = z.object({
  folderId: z.string().uuid(),
  name: z.string().min(1).optional(),
  color: z.string().optional(),
  icon: z.string().optional(),
  isPrivate: z.boolean().optional(),
  inheritStatus: z.boolean().optional(),
});
export type UpdateFolderRequest = z.infer<typeof updateFolderSchema>;

export const updateListSchema = z.object({
  listId: z.string().uuid(),
  name: z.string().min(1).optional(),
  color: z.string().optional(),
  icon: z.string().optional(),
  isPrivate: z.boolean().optional(),
  inheritStatus: z.boolean().optional(),
  startDate: z.string().datetime().optional(),
  dueDate: z.string().datetime().optional(),
});
export type UpdateListRequest = z.infer<typeof updateListSchema>;

export const updateEntityAccessBulkSchema = z.object({
  entityId: z.string().uuid(),
  layerType: z.number(), // EntityLayerType enum values
  members: z.array(updateMemberValueSchema),
});
export type UpdateEntityAccessBulkRequest = z.infer<
  typeof updateEntityAccessBulkSchema
>;
