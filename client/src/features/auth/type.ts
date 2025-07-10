import { User } from "@/types/user";
import z from "zod";

export const RegisterSchema = z.object({
  username: z
    .string()
    .min(1, "Username is required")
    .min(3, "Username must be at least 3 characters")
    .max(20, "Username must be less than 20 characters")
    .regex(
      /^[a-zA-Z0-9_]+$/,
      "Username can only contain letters, numbers, and underscores"
    ),
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email address"),
  password: z
    .string()
    .min(1, "Password is required")
    .min(8, "Password must be at least 8 characters")
    .regex(
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
      "Password must contain at least one uppercase letter, one lowercase letter, and one number"
    ),
});

export const LoginSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email address"),
  password: z
    .string()
    .min(1, "Password is required")
    .min(6, "Password must be at least 6 characters"),
});

export type LoginRequest = z.infer<typeof LoginSchema>;
export type RegisterCommand = z.infer<typeof RegisterSchema>;

export interface RegisterResponse {
  email: string;
  message: string;
}

export interface LoginResponse {
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  message: string;
}

export interface LogoutResponse {
  message: string;
}

export interface RefreshTokenResponse {
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  message: string;
}

export type MeResponse = User;
