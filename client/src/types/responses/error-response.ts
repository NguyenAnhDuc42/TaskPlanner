import { ProblemDetails } from "./problem-details";

export interface ErrorResponse extends ProblemDetails {
    // No additional properties needed if it's a direct mapping of ProblemDetails
    // If there were specific error response properties not covered by ProblemDetails, they would go here.
}