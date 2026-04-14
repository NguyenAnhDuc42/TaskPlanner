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
  [key: string]: any;
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
    this.code = data?.title || "UNKNOWN_ERROR";
    
    // 2. Extract remaining metadata into 'details', removing duplicates
    if (data) {
      const { title, status, detail, ...rest } = data;
      this.details = Object.keys(rest).length > 0 ? rest : null;
    } else {
      this.details = null;
    }

    // Ensure the prototype is set correctly for instanceof checks
    Object.setPrototypeOf(this, ApiError.prototype);
  }
}
