import { configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';
import { workspaceApi } from './workspaceApi';
import { spaceSlice, folderSlice, taskSlice, memberSlice, statusSlice, entityAccessSlice, assigneeSlice, workflowSlice, commentSlice, workspaceSlice, attachmentSlice, documentBlockSlice, favoriteSlice } from './entityStore';

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
    comments:      commentSlice.reducer,
    [workspaceSlice.name]: workspaceSlice.reducer,
    [attachmentSlice.name]: attachmentSlice.reducer,
    [documentBlockSlice.name]: documentBlockSlice.reducer,
    [favoriteSlice.name]: favoriteSlice.reducer,
    [workspaceApi.reducerPath]: workspaceApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false, // Turn off serialization check for maximum performance and flexible ISO date payloads
    }).concat(workspaceApi.middleware),
});

// Enable refetchOnFocus and refetchOnReconnect for active RTK Queries
setupListeners(store.dispatch);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
