namespace Api;

// WorkspaceId is genuinely HTTP-request-shaped (header/route/query) and is shared across
// middleware via HttpContext.Items — this constant keeps WorkspaceContextMiddleware (writer)
// and IdempotencyMiddleware (independent reader) from drifting apart on the raw string.
// CurrentMember is NOT stored here — see WorkspaceContext.cs for why.
public static class HttpContextItemKeys
{
    public const string WorkspaceId = "WorkspaceId";
}
