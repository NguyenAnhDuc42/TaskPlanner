import { configureStore } from '@reduxjs/toolkit';
import { spaceSlice, folderSlice, taskSlice, memberSlice, statusSlice } from './entityStore';
import { workspaceApi } from './workspaceApi';

export const store = configureStore({
  reducer: {
    spaces:   spaceSlice.reducer,
    folders:  folderSlice.reducer,
    tasks:    taskSlice.reducer,
    members:  memberSlice.reducer,
    statuses: statusSlice.reducer,
    [workspaceApi.reducerPath]: workspaceApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false, // Turn off serialization check for maximum performance and flexible ISO date payloads
    }).concat(workspaceApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
