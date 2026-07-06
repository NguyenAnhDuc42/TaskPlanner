import type { AxiosError } from "axios";

/**
 * Standard backend ProblemDetails response shape
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  // Machine-readable error code, always present now that every backend error producer
  // (Result-based endpoints, legacy controllers, WorkspaceContextMiddleware,
  // ExceptionHandlingMiddleware) writes to this same field — title is a human category,
  // never the code, on any of them anymore.
  code?: string;
  [key: string]: unknown;
}

/**
 * A normalized error object for all API failures.
 * Provides a clean, non-redundant interface for components.
 */
export class ApiError extends Error {
  public code: string;
  public status: number;
  public details: Omit<ProblemDetails, "title" | "status" | "detail"> | null;

  constructor(error: AxiosError<ProblemDetails>) {
    const data = error.response?.data;
    
    // 1. Extract the human-readable message
    const message = 
      data?.detail || 
      data?.title || 
      error.message || 
      "An unexpected error occurred";

    super(message);

    this.name = "ApiError";
    this.status = error.response?.status || 500;
    // `code` is the machine-readable identifier now — every backend producer sets it. Falling
    // back to `title` only covers a response that somehow didn't go through the shared shape.
    this.code = data?.code || data?.title || "UNKNOWN_ERROR";

    // 2. Extract remaining metadata into 'details', removing duplicates
    if (data) {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { title, status, detail, code, ...rest } = data;
      this.details = Object.keys(rest).length > 0 ? rest : null;
    } else {
      this.details = null;
    }

    // Ensure the prototype is set correctly for instanceof checks
    Object.setPrototypeOf(this, ApiError.prototype);
  }
}

/**
 * Safely extracts a human-readable message from an unknown error object.
 * Handles both standard ApiErrors and RTK Query wrapped errors without using 'any'.
 */
export function extractErrorMessage(error: unknown, fallback: string = "An unexpected error occurred."): string {
  if (!error || typeof error !== "object") return fallback;

  // 1. If it's an RTK Query onQueryStarted error wrapper: { error: { data: { message } } }
  if ("error" in error && typeof error.error === "object" && error.error !== null) {
    const rtkError = error.error as Record<string, unknown>;
    if ("data" in rtkError && typeof rtkError.data === "object" && rtkError.data !== null) {
      const data = rtkError.data as Record<string, unknown>;
      if (typeof data.message === "string") return data.message;
    }
  }

  // 2. If it's an RTK Query unwrap() error wrapper: { data: { message } }
  if ("data" in error && typeof error.data === "object" && error.data !== null) {
    const data = error.data as Record<string, unknown>;
    if (typeof data.message === "string") return data.message;
  }

  // 3. If it's an Error instance
  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallback;
}
