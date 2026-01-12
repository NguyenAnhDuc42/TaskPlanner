"use client";

import React, { createContext, useContext, useState } from "react";
import type { ContentPage, SidebarContextType } from "../type";

const SidebarContext = createContext<SidebarContextType | undefined>(undefined);

export function useSidebarContext() {
  const context = useContext(SidebarContext);
  if (!context) {
    throw new Error("useSidebarContext must be used within SidebarProvider");
  }
  return context;
}

interface SidebarProviderProps {
  children: React.ReactNode;
  defaultContent?: ContentPage;
  defaultOpen?: boolean;
}

export function SidebarProvider({
  children,
  defaultContent = "dashboard",
  defaultOpen = true,
}: SidebarProviderProps) {
  const [isInnerSidebarOpen, setIsInnerSidebarOpen] = useState(defaultOpen);
  const [activeContent, setActiveContent] =
    useState<ContentPage>(defaultContent);
  const [isHovering, setIsHovering] = useState(false);
  const [hoveredIcon, setHoveredIcon] = useState<ContentPage | null>(null);

  const toggleInnerSidebar = () => {
    setIsInnerSidebarOpen((prev) => !prev);
  };

  return (
    <SidebarContext.Provider
      value={{
        isInnerSidebarOpen,
        setIsInnerSidebarOpen,
        toggleInnerSidebar,
        activeContent,
        setActiveContent,
        isHovering,
        setIsHovering,
        hoveredIcon,
        setHoveredIcon,
      }}
    >
      {children}
    </SidebarContext.Provider>
  );
}
