import type React from "react"
import type { Metadata } from "next"
import "./globals.css"

import { TanStackQueryProvider } from "@/components/providers/tanstack-query-provider"
import WorkspaceUrlSync from "@/components/providers/workspace-url-sync"


export const metadata: Metadata = {
  title: "TaskPlanner",
  description: "A modern task planning application",
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body>
        <TanStackQueryProvider>
          <WorkspaceUrlSync />
                    {children}
        </TanStackQueryProvider>
      </body>
    </html>
  )
}