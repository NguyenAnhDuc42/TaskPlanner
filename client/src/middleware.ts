import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

const AUTH_PAGES = ["/authenthication"];
const PUBLIC_PATHS_PREFIXES = [
  "/_next",
  "/favicon.ico",
];

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const refreshToken = request.cookies.get("rft")?.value;

  if (PUBLIC_PATHS_PREFIXES.some((path) => pathname.startsWith(path))) {
    return NextResponse.next();
  }

  const hasRefreshToken = !!refreshToken;

  if (AUTH_PAGES.includes(pathname)) {
    if (hasRefreshToken) {
      console.log("Middleware: Refresh token found, redirecting from auth page to home.");
      return NextResponse.redirect(new URL("/", request.url));
    }
    console.log("Middleware: No refresh token, allowing access to auth page.");
    return NextResponse.next();
  }

  if (!hasRefreshToken) {
    console.log("Middleware: No refresh token found on protected page. Redirecting to auth.");
    return NextResponse.redirect(new URL("/authenthication", request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico).*)",
  ],
};