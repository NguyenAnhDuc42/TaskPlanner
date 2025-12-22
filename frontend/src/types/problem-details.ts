export class ProblemDetails extends Error {
  status?: number;
  type?: string;
  detail?: string;
  errors?: Record<string, Array<string>>; // For form-specific errors

  constructor(data: { status: number; title: string; detail?: string; errors?: any }) {
    super(data.title);
    this.status = data.status;
    this.detail = data.detail;
    this.errors = data.errors;
  }
}