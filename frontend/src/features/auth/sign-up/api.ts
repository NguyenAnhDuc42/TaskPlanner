import { useRegisterMutation } from "../api";
import { signUpSchema } from "./type";
import z from "zod";

type SignUpValues = z.infer<typeof signUpSchema>;

export function useRegister() {
  const [registerTrigger, { isLoading }] = useRegisterMutation();
  return {
    mutate: async (values: SignUpValues) => {
      return await registerTrigger(values).unwrap();
    },
    isPending: isLoading,
  };
}
