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
        },
        mutations: {
          onError: (error: unknown) => { 
            if (error instanceof AxiosError) {
              if (error.response?.status === 401) {
                console.error("TanStackQueryProvider Global Mutation Error: 401 Unauthorized. Session likely invalid.");
                authSessionManager.clearSession();
                client.removeQueries({ queryKey: AUTH_KEYS.me });
                toast.error("Your session has expired. Please log in again.");
              } else {
                console.error(`TanStackQueryProvider Global Mutation Error (${error.response?.status || 'Network'}):`, error.response?.data || error.message);
              }
            } else {
              console.error("TanStackQueryProvider Global Mutation Error (Non-Axios):", error);
            }
          },
        },
      },
    });
    client.getQueryCache().config.onError = (error: Error) => { 
        if (error instanceof AxiosError) {
          if (error.response?.status === 401) {
             console.warn("TanStackQueryProvider Global Query Cache Error: 401 Unauthorized (likely reactive refresh handled or session expired).");
          } else {
            console.error(`TanStackQueryProvider Global Query Cache Error (${error.response?.status || 'Network'}):`, error.response?.data || error.message);
          }
        } else {
          console.error("TanStackQueryProvider Global Query Cache Error (Non-Axios):", error);
        }
    };

    return client;
  });

  useEffect(() => {
    authSessionManager.setQueryClient(queryClient);
  }, [queryClient]);

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}