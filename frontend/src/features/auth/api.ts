import { workspaceApi } from "@/store/workspaceApi";
import { getCookie } from "@/lib/cookie-utils";
import type { User } from "./types";

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export interface RegisterResponse {
  user: User;
}

export interface LoginRequest {
  email?: string;
  password?: string;
}

export interface RegisterRequest {
  name?: string;
  email?: string;
  password?: string;
}

export const authApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getMe: build.query<User | null, void>({
      query: () => ({ url: "/auth/me", method: "GET" }),
      providesTags: ["User"],
    }),
    login: build.mutation<LoginResponse, LoginRequest>({
      query: (values) => ({ url: "/auth/login", method: "POST", data: values }),
      invalidatesTags: ["User", "UserPreference"],
    }),
    register: build.mutation<RegisterResponse, RegisterRequest>({
      query: (values) => ({
        url: "/auth/register",
        method: "POST",
        data: {
          ...values,
          userName: values.name,
        },
      }),
      invalidatesTags: ["UserPreference"],
    }),
    logout: build.mutation<void, void>({
      query: () => ({ url: "/auth/logout", method: "POST" }),
      invalidatesTags: ["User"],
    }),
    updateProfile: build.mutation<User, { name?: string; email?: string }>({
      query: (payload) => ({ url: "/auth/profile", method: "PUT", data: payload }),
      invalidatesTags: ["User"],
    }),
    changePassword: build.mutation<void, { currentPassword: string; newPassword: string }>({
      query: (payload) => ({ url: "/auth/change-password", method: "POST", data: payload }),
    }),
  }),
});

export const {
  useGetMeQuery,
  useLoginMutation,
  useRegisterMutation,
  useLogoutMutation,
  useUpdateProfileMutation,
  useChangePasswordMutation,
} = authApi;

export const authKeys = {
  all: ["auth"] as const,
  me: () => [...authKeys.all, "me"] as const,
};

export function useUser() {
  const isLoggedIn = !!getCookie("is_logged_in");
  const { data, isLoading, isFetching } = useGetMeQuery(undefined, {
    skip: !isLoggedIn,
  });

  return {
    data,
    isLoading,
    isFetching,
    status: !isLoggedIn ? "error" : (isLoading || isFetching ? "pending" : data ? "success" : "error") as "error" | "pending" | "success",
  };
}

export function useLogout() {
  const [logoutTrigger] = useLogoutMutation();
  return {
    mutate: async () => {
      await logoutTrigger().unwrap();
      window.location.href = "/auth/sign-in";
    },
  };
}

export function useUpdateProfile() {
  const [updateProfileTrigger] = useUpdateProfileMutation();
  return {
    mutate: (payload: { name?: string; email?: string }) => updateProfileTrigger(payload).unwrap(),
  };
}

export function useChangePassword() {
  const [changePasswordTrigger] = useChangePasswordMutation();
  return {
    mutate: (payload: { currentPassword: string; newPassword: string }) => changePasswordTrigger(payload).unwrap(),
  };
}
