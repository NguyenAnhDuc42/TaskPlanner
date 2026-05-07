import { format } from "date-fns";

interface TelemetrySectionProps {
  activities: any[];
}

export function TelemetrySection({ activities }: TelemetrySectionProps) {
  return (
    <div className="space-y-4 px-1">
      {activities?.length > 0 ? (
        activities.slice(0, 5).map((activity: any) => (
          <ActivityRow 
            key={activity.id} 
            label={activity.content} 
            time={format(new Date(activity.timestamp), "h:mm a")} 
          />
        ))
      ) : (
        <div className="text-[10px] text-muted-foreground/20 italic font-bold">
          No recent telemetry
        </div>
      )}
    </div>
  );
}

function ActivityRow({ label, time }: { label: string, time: string }) {
  return (
    <div className="flex items-center justify-between group py-1">
      <div className="flex items-center gap-3 overflow-hidden">
        <div className="h-1 w-1 rounded-full bg-muted-foreground/10 group-hover:bg-primary transition-colors flex-shrink-0" />
        <span className="text-[10px] font-bold text-muted-foreground/60 group-hover:text-foreground transition-colors truncate">{label}</span>
      </div>
      <span className="text-[10px] font-mono text-muted-foreground/20 flex-shrink-0">{time}</span>
    </div>
  );
}
