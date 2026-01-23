export type MembershipStatus = "Pending" | "Active" | "Invited" | "Suspended";

export const MEMBERSHIP_STATUS_LABELS: Record<MembershipStatus, string> = {
  Pending: "Pending",
  Active: "Active",
  Invited: "Invited",
  Suspended: "Suspended",
};

export function getMembershipStatusLabel(status: MembershipStatus) {
  return MEMBERSHIP_STATUS_LABELS[status] ?? status;
}
