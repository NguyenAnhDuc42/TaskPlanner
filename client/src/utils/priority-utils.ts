export enum Priority {
  Urgent = "Urgent",
  High = "High",
  Medium = "Medium",
  Low = "Low",
  Clear = "Clear",
}

export function mapPriorityFromApi(priority: number): Priority {
  switch (priority) {
    case 0:
      return Priority.Urgent;
    case 1:
      return Priority.High;
    case 2:
      return Priority.Medium;
    case 3:
      return Priority.Low;
    case 4:
      return Priority.Clear;
    default:
      return Priority.Clear;
  }
}

export function mapPriorityToApi(priority: Priority): number {
  switch (priority) {
    case Priority.Urgent:
      return 0;
    case Priority.High:
      return 1;
    case Priority.Medium:
      return 2;
    case Priority.Low:
      return 3;
    case Priority.Clear:
      return 4;
    default:
      return 4;
  }
}

export interface BadgeContext {
  priorityName: string;
  badgeClasses: string;
}

export function mapPriorityToBadge(priority: Priority | null): BadgeContext {
  switch (priority) {
    case Priority.Urgent:
      return {
        priorityName: "Urgent",
        badgeClasses: "bg-red-400 text-yellow-900 hover:bg-yellow-500",
      };
    case Priority.High:
      return {
        priorityName: "High",
        badgeClasses: "bg-orange-500 text-white hover:bg-red-600",
      };
    case Priority.Medium:
      return {
        priorityName: "Medium",
        badgeClasses: "bg-yellow-500 text-white hover:bg-blue-600",
      };
    case Priority.Low:
      return {
        priorityName: "Low",
        badgeClasses: "bg-green-500 text-gray-900 hover:bg-gray-500",
      };
    case Priority.Clear:
      return {
        priorityName: "Clear",
        badgeClasses: "bg-gray-500 text-gray-900 hover:bg-gray-500",
      };
    default:
      return {
        priorityName: "Unknown",
        badgeClasses: "bg-gray-500 text-gray-800",
      };
  }
}

export function mapRoleToAvatarStyle(priority: Priority | null): string {
  switch (priority) {
    case Priority.Urgent:
      return "bg-red-500 text-white";
    case Priority.High:
      return "bg-orange-500 text-white";
    case Priority.Medium:
      return "bg-yellow-500 text-white";
    case Priority.Low:
      return "bg-green-500 text-white";
    case Priority.Clear:
      return "bg-gray-500 text-white";
    default:
      return "bg-gray-500 text-gray-800";
  }
}
