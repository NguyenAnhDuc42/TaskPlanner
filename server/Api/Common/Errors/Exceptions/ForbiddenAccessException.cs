namespace Api;

// Authenticated but not permitted — distinct from UnauthorizedAccessException (which
// ExceptionHandlingMiddleware maps to 401, reserved for "not logged in" cases). A Guest hitting
// a Member-only action is a 403, not a 401 — conflating the two made the frontend's 401 handler
// attempt a session refresh + retry for what was actually a permission problem.
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
