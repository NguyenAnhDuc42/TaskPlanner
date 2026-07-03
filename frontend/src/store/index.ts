import { configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';
import { workspaceApi } from './workspaceApi';
import { workspaceSlice } from './entityStore';

export const store = configureStore({
  reducer: {
    [workspaceSlice.name]: workspaceSlice.reducer,
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
