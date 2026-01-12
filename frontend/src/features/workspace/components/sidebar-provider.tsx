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
  initialWorkspaceId?: string;
}

export function SidebarProvider({
  children,
  defaultContent = "dashboard",
  defaultOpen = true,
  initialWorkspaceId,
}: SidebarProviderProps) {
  const [isInnerSidebarOpen, setIsInnerSidebarOpen] = useState(defaultOpen);
  const [activeContent, setActiveContent] =
    useState<ContentPage>(defaultContent);
  const [isHovering, setIsHovering] = useState(false);
  const [hoveredIcon, setHoveredIcon] = useState<ContentPage | null>(null);
  const [workspaceId, setWorkspaceId] = useState<string | null>(
    initialWorkspaceId || "default-workspace"
  );
  const [sidebarContent, setSidebarContent] = useState<React.ReactNode | null>(
    null
  );

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
        workspaceId,
        setWorkspaceId,
        sidebarContent,
        setSidebarContent,
      }}
    >
      {children}
    </SidebarContext.Provider>
  );
}
