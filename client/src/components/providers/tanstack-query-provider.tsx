// src/components/providers/tanstack-query-provider.tsx
"use client";

import { QueryClient, QueryClientProvider, } from "@tanstack/react-query"; // Import QueryCache
import { useState, useEffect } from "react";

import { AxiosError } from "axios";
import { toast } from "sonner";
import { authSessionManager } from "@/features/auth/auth-session-manager";
import { AUTH_KEYS } from "@/features/auth/hooks";

export function TanStackQueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => {
    // Create the QueryClient instance
    const client = new QueryClient({
      defaultOptions: {
        queries: {
          staleTime: 5 * 60 * 1000,
          retry: 1,
          refetchOnWindowFocus: true,
          refetchOnMount: true,
          refetchOnReconnect: true,
          // Removed: onError directly here. Global query errors handled via QueryCache below.
        },
        mutations: {
          // onError for mutations is still valid directly here
          onError: (error: unknown) => { // Use unknown for initial type, then narrow down
            if (error instanceof AxiosError) {
              if (error.response?.status === 401) {
                console.error("TanStackQueryProvider Global Mutation Error: 401 Unauthorized. Session likely invalid.");
                authSessionManager.clearSession();
                client.removeQueries({ queryKey: AUTH_KEYS.me }); // Use `client` here
                toast.error("Your session has expired. Please log in again.");
              } else {
                console.error(`TanStackQueryProvider Global Mutation Error (${error.response?.status || 'Network'}):`, error.response?.data || error.message);
                // toast.error(`Operation failed: ${error.response?.data?.detail || error.message || 'Please try again.'}`);
              }
            } else {
              console.error("TanStackQueryProvider Global Mutation Error (Non-Axios):", error);
            }
          },
        },
      },
    });

    // Set up a global error listener for ALL queries on the QueryCache
    // This is the correct way to globally handle query errors in v5
    client.getQueryCache().config.onError = (error: Error) => { // Type as Error for standard errors
        if (error instanceof AxiosError) {
          // For 401s, `AuthProvider`'s useEffect will handle the `useUser` query.
          // This global handler can catch 401s from other queries if they occur,
          // or handle other non-auth-related query errors.
          if (error.response?.status === 401) {
             console.warn("TanStackQueryProvider Global Query Cache Error: 401 Unauthorized (likely reactive refresh handled or session expired).");
             // No toast here as AuthProvider handles useUser's 401, avoiding duplicate.
             // If you want a catch-all logout for any query 401, you could put it here:
             // authSessionManager.clearSession();
             // client.removeQueries({ queryKey: AUTH_KEYS.me });
             // toast.error("Your session expired during an operation. Please log in again.");
          } else {
            console.error(`TanStackQueryProvider Global Query Cache Error (${error.response?.status || 'Network'}):`, error.response?.data || error.message);
            // toast.error(`Error fetching data: ${error.response?.data?.detail || error.message || 'Please try again.'}`);
          }
        } else {
          console.error("TanStackQueryProvider Global Query Cache Error (Non-Axios):", error);
        }
    };

    return client;
  });

  // Initialize the AuthSessionManager with the queryClient instance once
  useEffect(() => {
    authSessionManager.setQueryClient(queryClient);
  }, [queryClient]);

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}