import type React from "react"
import type { Metadata } from "next"
import "./globals.css"

import { TanStackQueryProvider } from "@/components/providers/tanstack-query-provider"


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
      <body className="scrollbars">
        <TanStackQueryProvider>
                    {children}
        </TanStackQueryProvider>
      </body>
    </html>
  )
}