"use client";

export default function SettingsIndex() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
        <p className="text-muted-foreground">
          Manage your workspace preferences.
        </p>
      </div>

      <div className="space-y-4">
        {["General", "Security", "Notifications", "Billing"].map((tab) => (
          <div
            key={tab}
            className="rounded-lg border bg-card p-4 flex items-center justify-between"
          >
            <span className="font-medium">{tab}</span>
            <span className="text-sm text-muted-foreground">Configure</span>
          </div>
        ))}
      </div>
    </div>
  );
}
