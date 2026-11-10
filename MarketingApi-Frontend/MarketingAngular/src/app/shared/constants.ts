export const APP_ROLES = {
  Admin: 'Admin',
  SupervisorMaster: 'Supervisor Master',
  MarketingMaster: 'Marketing Master',
} as const;

export enum DeactivationRequestStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Cancelled = 'Cancelled',
}

export enum TeamRoleType {
  Leader = 'Leader',
  Member = 'Member',
}

export type SortDirection = 'asc' | 'desc';
