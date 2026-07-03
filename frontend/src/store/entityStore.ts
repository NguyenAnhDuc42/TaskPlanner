import { createEntityAdapter, createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { WorkspaceSnippetRecord } from "@/types/workspace";
import type { RootState } from "./index";

// ─── UTILITY: GRANULAR DEEP MERGE (LAST WRITE WINS) ───
function safeMergeEntity<T extends { id: string }>(
  existing: T | undefined,
  incoming: Partial<T>
): T {
  if (!existing) return incoming as T;

  const merged = { ...existing };

  (Object.keys(incoming) as Array<keyof T>).forEach((key) => {
    const val = incoming[key];
    if (val !== undefined) {
      if (val === null && (key === "hasTasks" || key === "hasFolders" || key === "hasChildren")) {
        return;
      }
      if (Array.isArray(val)) {
        merged[key] = [...val] as unknown as T[keyof T];
      } else if (val && typeof val === 'object') {
        const existingNested = merged[key] as Record<string, unknown> | undefined;
        merged[key] = {
          ...(existingNested || {}),
          ...(val as Record<string, unknown>),
        } as unknown as T[keyof T];
      } else {
        merged[key] = val as unknown as T[keyof T];
      }
    }
  });

  return merged;
}

// Space/Folder/Task/Member/Status/Assignee/Comment/Attachment/DocumentBlock/Notification all used
// to have Redux slices here too — removed once each was fully migrated to the sync engine
// (MobX rootStore, IndexedDB, SignalR Delta/DeltaBatch — see FRONTEND_SYNC_CONTEXT.md). Workspace
// is the one entity still genuinely on Redux/RTK Query: the home-screen workspace list/switcher
// is a separate concern from per-workspace Bootstrap data and isn't part of the sync engine's scope.
export const adapters = {
  workspaces: createEntityAdapter<WorkspaceSnippetRecord>(),
};

export const workspaceSlice = createSlice({
  name: 'workspaces',
  initialState: adapters.workspaces.getInitialState(),
  reducers: {
    upsert(state, action: PayloadAction<Partial<WorkspaceSnippetRecord> & { id: string }>) {
      const existing = state.entities[action.payload.id];
      const merged = safeMergeEntity(existing, action.payload);
      adapters.workspaces.upsertOne(state, merged);
    },
    upsertMany(state, action: PayloadAction<Partial<WorkspaceSnippetRecord>[]>) {
      action.payload.forEach((item) => {
        if (!item.id) return;
        const existing = state.entities[item.id];
        const merged = safeMergeEntity(existing, item);
        adapters.workspaces.upsertOne(state, merged);
      });
    },
    remove: adapters.workspaces.removeOne,
    removeMany: adapters.workspaces.removeMany,
  }
});

export const workspaceSelectors = adapters.workspaces.getSelectors((s: RootState) => s.workspaces);
