import { useMutation, useQueryClient } from "@tanstack/react-query";
import { signInSchema } from "./type";
import z from "zod";
import { authKeys } from "../api";
import { api } from "@/lib/api-client";
import type { LoginResponse } from "../types";

type SignInValues = z.infer<typeof signInSchema>;

export function useLogin() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (values: SignInValues) => {
      const { data } = await api.post<LoginResponse>("/auth/login", values);
      return data;
    },
    onSuccess: async () => {
      await queryClient.refetchQueries({ queryKey: authKeys.me() });
    },
  });
}
