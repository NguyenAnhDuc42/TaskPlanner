import { UserMenuBar } from "@/components/auth/user-menu-bar";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { UserDetail } from "@/types/user";
import { Plus, Search } from "lucide-react";

interface NavbarProps {
  currentUser: UserDetail;
  onAddWorkspace: () => void;
}

export function MainPageNavbar({ 
  currentUser,
  onAddWorkspace
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
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <Input
                type="text"
                placeholder="Search..."
                className="w-full pl-10 pr-4 py-2 bg-gray-900 border-gray-700 text-white placeholder-gray-400 focus:border-white focus:ring-white"
              />
            </div>
          </div>

          {/* Right side - Add button and User */}
          <div className="flex items-center gap-4">
            {/* Add Workspace Button */}
            <Button
              onClick={onAddWorkspace}
              className="bg-white text-black hover:bg-gray-200 flex items-center gap-2"
            >
              <Plus className="h-4 w-4" />
              Add Workspace
            </Button>

            {/* User Menu */}
            <UserMenuBar
              currentUser={currentUser}
            />
          </div>
        </div>
      </div>
    </nav>
  );
}