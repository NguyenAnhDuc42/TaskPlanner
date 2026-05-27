import { useLoginMutation } from "../api";
import { signInSchema } from "./type";
import z from "zod";

type SignInValues = z.infer<typeof signInSchema>;

export function useLogin() {
  const [loginTrigger] = useLoginMutation();
  return {
    mutate: async (values: SignInValues) => {
      return await loginTrigger(values).unwrap();
    },
  };
}
