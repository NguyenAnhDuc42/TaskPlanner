import { createEntityAdapter, createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { MemberRecord } from "@/types/workspace/member-record";
import type { Status } from "@/types/status";
import type { RootState } from './index';

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

export const adapters = {
  spaces:   createEntityAdapter<SpaceRecord>(),
  folders:  createEntityAdapter<FolderRecord>(),
  tasks:    createEntityAdapter<TaskRecord>(),
  members:  createEntityAdapter<MemberRecord>(),
  statuses: createEntityAdapter<Status>(),
};

// ─── REDUX SLICES WITH SAFE MERGE TRANSACTIONS (Strict Typed + removeMany) ───
export const spaceSlice = createSlice({
  name: 'spaces',
  initialState: adapters.spaces.getInitialState(),
  reducers: {
    upsert(state, action: PayloadAction<Partial<SpaceRecord> & { id: string }>) {
      const existing = state.entities[action.payload.id];
      const merged = safeMergeEntity(existing, action.payload);
      adapters.spaces.upsertOne(state, merged);
    },
    upsertMany(state, action: PayloadAction<Partial<SpaceRecord>[]>) {
      action.payload.forEach((item) => {
        if (!item.id) return;
        const existing = state.entities[item.id];
        const merged = safeMergeEntity(existing, item);
        adapters.spaces.upsertOne(state, merged);
      });
    },
    remove: adapters.spaces.removeOne,
    removeMany: adapters.spaces.removeMany,
  }
});

export const taskSlice = createSlice({
  name: 'tasks',
  initialState: adapters.tasks.getInitialState(),
  reducers: {
    upsert(state, action: PayloadAction<Partial<TaskRecord> & { id: string }>) {
      const existing = state.entities[action.payload.id];
      const merged = safeMergeEntity(existing, action.payload);
      adapters.tasks.upsertOne(state, merged);
    },
    upsertMany(state, action: PayloadAction<Partial<TaskRecord>[]>) {
      action.payload.forEach((item) => {
        if (!item.id) return;
        const existing = state.entities[item.id];
        const merged = safeMergeEntity(existing, item);
        adapters.tasks.upsertOne(state, merged);
      });
    },
    remove: adapters.tasks.removeOne,
    removeMany: adapters.tasks.removeMany,
  }
});

export const folderSlice = createSlice({
  name: 'folders',
  initialState: adapters.folders.getInitialState(),
  reducers: {
    upsert(state, action: PayloadAction<Partial<FolderRecord> & { id: string }>) {
      const existing = state.entities[action.payload.id];
      const merged = safeMergeEntity(existing, action.payload);
      adapters.folders.upsertOne(state, merged);
    },
    upsertMany(state, action: PayloadAction<Partial<FolderRecord>[]>) {
      action.payload.forEach((item) => {
        if (!item.id) return;
        const existing = state.entities[item.id];
        const merged = safeMergeEntity(existing, item);
        adapters.folders.upsertOne(state, merged);
      });
    },
    remove: adapters.folders.removeOne,
    removeMany: adapters.folders.removeMany,
  }
});

export const memberSlice = createSlice({
  name: 'members',
  initialState: adapters.members.getInitialState(),
  reducers: {
    upsert(state, action: PayloadAction<Partial<MemberRecord> & { id: string }>) {
      const existing = state.entities[action.payload.id];
      const merged = safeMergeEntity(existing, action.payload);
      adapters.members.upsertOne(state, merged);
    },
    upsertMany(state, action: PayloadAction<Partial<MemberRecord>[]>) {
      action.payload.forEach((item) => {
        if (!item.id) return;
        const existing = state.entities[item.id];
        const merged = safeMergeEntity(existing, item);
        adapters.members.upsertOne(state, merged);
      });
    },
    remove: adapters.members.removeOne,
    removeMany: adapters.members.removeMany,
  }
});

export const statusSlice = createSlice({
  name: 'statuses',
  initialState: adapters.statuses.getInitialState(),
  reducers: {
    upsert(state, action: PayloadAction<Partial<Status> & { id: string }>) {
      const existing = state.entities[action.payload.id];
      const merged = safeMergeEntity(existing, action.payload);
      adapters.statuses.upsertOne(state, merged);
    },
    upsertMany(state, action: PayloadAction<Partial<Status>[]>) {
      action.payload.forEach((item) => {
        if (!item.id) return;
        const existing = state.entities[item.id];
        const merged = safeMergeEntity(existing, item);
        adapters.statuses.upsertOne(state, merged);
      });
    },
    remove: adapters.statuses.removeOne,
    removeMany: adapters.statuses.removeMany,
  }
});

// Central selectors
export const spaceSelectors  = adapters.spaces.getSelectors((s: RootState) => s.spaces);
export const folderSelectors = adapters.folders.getSelectors((s: RootState) => s.folders);
export const taskSelectors   = adapters.tasks.getSelectors((s: RootState) => s.tasks);
export const memberSelectors = adapters.members.getSelectors((s: RootState) => s.members);
export const statusSelectors = adapters.statuses.getSelectors((s: RootState) => s.statuses);
