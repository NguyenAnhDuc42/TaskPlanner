using System;
using src.Helper.Middleware;

namespace src.Helper.Extensions;

public static class ExtensionHandlingExtension
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
