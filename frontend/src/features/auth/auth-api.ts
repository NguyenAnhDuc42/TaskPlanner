import { workspaceApi } from "@/store/workspaceApi";
import { getCookie } from "@/lib/cookie-utils";
import type { User } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

// ─── RTK Query Endpoints ──────────────────────────────────────────────────────

export const authApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getMe: build.query<User | null, void>({
      query: () => ({ url: "/auth/me", method: "GET" }),
      providesTags: ["User"],
    }),
    login: build.mutation<void, LoginRequest>({
      query: (values) => ({ url: "/auth/login", method: "POST", data: values }),
      invalidatesTags: ["User", "UserPreference"],
    }),
    register: build.mutation<void, RegisterRequest>({
      query: (values) => ({
        url: "/auth/register",
        method: "POST",
        data: { ...values, userName: values.name },
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
    forgotPassword: build.mutation<void, { email: string }>({
      query: (payload) => ({ url: "/auth/forgot-password", method: "POST", data: payload }),
    }),
    resetPassword: build.mutation<void, { token: string; newPassword: string }>({
      query: (payload) => ({ url: "/auth/reset-password", method: "POST", data: payload }),
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
  useForgotPasswordMutation,
  useResetPasswordMutation,
} = authApi;

// ─── Hooks ────────────────────────────────────────────────────────────────────

export function useUser() {
  const isLoggedIn = !!getCookie("is_logged_in");
  const { data, isLoading, isFetching } = useGetMeQuery(undefined, {
    skip: !isLoggedIn,
  });

  return {
    data,
    isLoading,
    isFetching,
    status: !isLoggedIn
      ? "error"
      : isLoading || isFetching
        ? "pending"
        : data
          ? "success"
          : ("error" as "error" | "pending" | "success"),
  };
}

export function useLogin() {
  const [trigger, { isLoading }] = useLoginMutation();
  return {
    mutate: (values: LoginRequest) => trigger(values).unwrap(),
    isPending: isLoading,
  };
}

export function useRegister() {
  const [trigger, { isLoading }] = useRegisterMutation();
  return {
    mutate: (values: RegisterRequest) => trigger(values).unwrap(),
    isPending: isLoading,
  };
}

export function useLogout() {
  const [trigger] = useLogoutMutation();
  return {
    mutate: async () => {
      await trigger().unwrap();
      window.location.href = "/auth/sign-in";
    },
  };
}

export function useForgotPassword() {
  const [trigger, { isLoading }] = useForgotPasswordMutation();
  return {
    mutate: (email: string) => trigger({ email }).unwrap(),
    isPending: isLoading,
  };
}

export function useResetPassword() {
  const [trigger, { isLoading }] = useResetPasswordMutation();
  return {
    mutate: (token: string, newPassword: string) => trigger({ token, newPassword }).unwrap(),
    isPending: isLoading,
  };
}

export function useUpdateProfile() {
  const [trigger] = useUpdateProfileMutation();
  return {
    mutate: (payload: { name?: string; email?: string }) => trigger(payload).unwrap(),
  };
}

export function useChangePassword() {
  const [trigger] = useChangePasswordMutation();
  return {
    mutate: (payload: { currentPassword: string; newPassword: string }) => trigger(payload).unwrap(),
  };
}
