import { type ReactNode, useState, useCallback } from "react";
import { WorkspaceSessionContext } from "./workspace-session";
import {
  useUserPreference,
  useUpdateUserPreference,
} from "@/features/main/user-preference-api";
import type { ContentPage } from "../type";

interface WorkspaceSessionProviderProps {
  children: ReactNode;
  workspaceId: string;
}

export function WorkspaceSessionProvider({
  children,
  workspaceId,
}: WorkspaceSessionProviderProps) {
  const { data: preferences } = useUserPreference();
  const { mutate: updatePreferences } = useUpdateUserPreference();

  // Read saved widths from user preferences (per-workspace)
  const wsSettings = preferences?.workspaceSettings?.[workspaceId];
  const savedSidebarWidth = wsSettings?.sideBarWidth ?? 260;
  const savedContextWidth = wsSettings?.contextContentWidth ?? 350;

  const [activeIcon, setActiveIcon] = useState<ContentPage | null>(null);
  const [isInnerSidebarOpen, setIsInnerSidebarOpen] = useState(
    wsSettings?.isSidebarOpen ?? true,
  );
  const [hoveredIcon, setHoveredIcon] = useState<ContentPage | null>(null);
  const [sidebarWidth, setSidebarWidth] = useState(savedSidebarWidth);
  const [contextWidth, setContextWidth] = useState(savedContextWidth);

  // ─── Actions ──────────────────────────────────────────
  const selectIcon = useCallback((icon: ContentPage | null) => {
    setActiveIcon(icon);
    if (icon) {
      setIsInnerSidebarOpen(true);
    } else {
      setIsInnerSidebarOpen(false);
    }
    setHoveredIcon(null);
  }, []);

  const toggleInnerSidebar = useCallback(() => {
    setIsInnerSidebarOpen((prev) => !prev);
  }, []);

  const updateSidebarWidth = useCallback(
    (newWidth: number) => {
      setSidebarWidth(newWidth);
      updatePreferences({
        workspaceSettings: {
          [workspaceId]: {
            ...wsSettings,
            sideBarWidth: newWidth,
          },
        },
      } as any);
    },
    [workspaceId, wsSettings, updatePreferences],
  );

  const updateContextWidth = useCallback(
    (newWidth: number) => {
      setContextWidth(newWidth);
      updatePreferences({
        workspaceSettings: {
          [workspaceId]: {
            ...wsSettings,
            contextContentWidth: newWidth,
          },
        },
      } as any);
    },
    [workspaceId, wsSettings, updatePreferences],
  );

  return (
    <WorkspaceSessionContext.Provider
      value={{
        state: {
          activeIcon,
          isInnerSidebarOpen,
          hoveredIcon,
          sidebarWidth,
          contextWidth,
        },
        actions: {
          selectIcon,
          toggleInnerSidebar,
          setHoveredIcon,
          updateSidebarWidth,
          updateContextWidth,
        },
      }}
    >
      {children}
    </WorkspaceSessionContext.Provider>
  );
}
