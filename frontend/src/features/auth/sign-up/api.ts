import { useMutation } from "@tanstack/react-query";
import { signUpSchema } from "./type"; // This is likely still ./type in sign-up folder? Check list_dir
import z from "zod";
import { api } from "@/lib/api-client";
import type { RegisterResponse } from "../types";

// Wait, I need to check sign-up/type.ts vs types.ts local
type SignUpValues = z.infer<typeof signUpSchema>;

export function useRegister() {
  return useMutation({
    mutationFn: async (values: SignUpValues) => {
      const { data } = await api.post<RegisterResponse>("/auth/register", {
        ...values,
        userName: values.name,
      });
      return data;
    },
  });
}
