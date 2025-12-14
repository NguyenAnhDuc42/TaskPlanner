
import React from 'react';

const WorkspaceHeaderBar = () => {
  return (
    <header className="flex items-center justify-between p-4 border-b">
      <div>
        {/* Breadcrumb will go here */}
        <p className="text-sm text-muted-foreground">Breadcrumb</p>
      </div>
      <div className="flex items-center space-x-2">
        {/* Buttons will go here */}
        <button className="px-3 py-1 text-sm border rounded-md">Button 1</button>
        <button className="px-3 py-1 text-sm border rounded-md">Button 2</button>
      </div>
    </header>
  );
};

export default WorkspaceHeaderBar;
