import { configureStore } from '@reduxjs/toolkit';
import { workspaceApi } from './workspaceApi';
import { spaceSlice, folderSlice, taskSlice, memberSlice, statusSlice, entityAccessSlice, assigneeSlice, workflowSlice, commentSlice } from './entityStore';

export const store = configureStore({
  reducer: {
    spaces:   spaceSlice.reducer,
    folders:  folderSlice.reducer,
    tasks:    taskSlice.reducer,
    members:  memberSlice.reducer,
    statuses: statusSlice.reducer,
    entityAccess: entityAccessSlice.reducer,
    assignees: assigneeSlice.reducer,
    workflows: workflowSlice.reducer,
    comments: commentSlice.reducer,
    [workspaceApi.reducerPath]: workspaceApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false, // Turn off serialization check for maximum performance and flexible ISO date payloads
    }).concat(workspaceApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
