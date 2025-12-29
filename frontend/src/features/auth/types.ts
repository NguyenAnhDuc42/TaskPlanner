export interface User {
  id: string;
  email: string;
  name: string;
}

export type LoginResponse = void;
export type RegisterResponse = void;

// RFC 7807 Problem Details
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}
