import { createContext, useContext, type ReactNode } from "react";
import type { User } from "./types";
import { useLogout, useUser } from "./api";

export interface AuthContextType {
  user: User | null | undefined;
  isLoading: boolean;
  isAuthenticated: boolean;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const { data: user, status, isFetching } = useUser();
  const logoutMutation = useLogout();

  console.log(user);

  // isLoading is true only on the very first load or when refetching without data
  const isLoading = status === "pending" || (isFetching && !user);
  const isAuthenticated = !!user && status === "success";

  const value = {
    user,
    isLoading,
    isAuthenticated,
    logout: () => logoutMutation.mutate(),
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
