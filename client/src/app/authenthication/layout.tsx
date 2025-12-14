import type React from "react"

export default function AuthenticationLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    // This layout will simply render its children.
    // The specific styling for your authentication pages should be defined
    // directly within your page.tsx components (e.g., app/Authentication/page.tsx).
    <>
      {children}
    </>
  )
}