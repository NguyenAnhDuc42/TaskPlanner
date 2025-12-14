"use client"

import type React from "react"

import { useState } from "react"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

interface AnimatedTabsProps {
  children: React.ReactNode
  defaultValue: string
}

export function AnimatedTabs({ children, defaultValue }: AnimatedTabsProps) {
  const [activeTab, setActiveTab] = useState(defaultValue)

  return (
    <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
      <div className="relative">
        <TabsList className="grid w-full grid-cols-2 bg-zinc-800 relative overflow-hidden">
          {/* Sliding indicator */}
          <div
            className={`absolute top-1 bottom-1 left-1 right-1 bg-zinc-100 rounded-sm transition-transform duration-300 ease-out ${
              activeTab === "login" ? "translate-x-0" : "translate-x-full"
            }`}
            style={{ width: "calc(50% - 4px)" }}
          />
          <TabsTrigger
            value="login"
            className="relative z-10 data-[state=active]:bg-transparent data-[state=active]:text-zinc-900 text-zinc-400 transition-colors duration-300"
          >
            Sign In
          </TabsTrigger>
          <TabsTrigger
            value="register"
            className="relative z-10 data-[state=active]:bg-transparent data-[state=active]:text-zinc-900 text-zinc-400 transition-colors duration-300"
          >
            Sign Up
          </TabsTrigger>
        </TabsList>
      </div>
      {children}
    </Tabs>
  )
}

export function AnimatedTabsContent({ value, children }: { value: string; children: React.ReactNode }) {
  const slideDirection = value === "login" ? "slide-in-from-left-2" : "slide-in-from-right-2"

  return (
    <TabsContent
      value={value}
      className={`space-y-3 mt-4 data-[state=active]:animate-in data-[state=active]:${slideDirection} data-[state=active]:fade-in-0 data-[state=active]:duration-300`}
    >
      {children}
    </TabsContent>
  )
}
