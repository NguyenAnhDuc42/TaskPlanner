import { UserMenuBar } from "@/components/auth/user-menu-bar";
import { CreateWorkspaceDialog } from "@/components/custom-form/buttons/create-workspace-dialog";
import { Button } from "@/components/ui/button";
import { UserDetail } from "@/types/user";
import { Plus } from "lucide-react";
import { JoinWorkspaceInput } from "./join-workspace-input";

interface NavbarProps {
  currentUser: UserDetail;
}

export function MainPageNavbar({ 
  currentUser,
}: NavbarProps) {
  return (
    <nav className="bg-black border-b border-gray-800 sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-8 py-4">
        <div className="flex items-center justify-between gap-6">
          {/* Logo/Brand */}
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-white rounded-lg flex items-center justify-center">
              <div className="w-4 h-4 bg-black rounded-sm"></div>
            </div>
            <span className="text-white text-lg font-medium">
              Workspace
            </span>
          </div>

          {/* Search Bar */}
          <div className="flex-1 max-w-md">
            <div className="relative">
              <JoinWorkspaceInput/>
            </div>
          </div>

          {/* Right side - Add button and User */}
          <div className="flex items-center gap-4 h-full">
            {/* Add Workspace Button */}
            <CreateWorkspaceDialog>
              <Button className="bg-white text-black hover:bg-gray-200 flex items-center gap-2">
                <Plus className="h-4 w-4" />
                Add Workspace
              </Button>
            </CreateWorkspaceDialog>

            {/* User Menu */}
            <UserMenuBar
              currentUser={currentUser}
              className=""
            />
          </div>
        </div>
      </div>
    </nav>
  );
}