export interface Salesman {
  id: number;
  salesmancode: string;
  salesmanname: string;
  mobileno?: string;
  email?: string;
  joiningdate?: string;
  status: number;
  statusName?: string;
  hasPendingDeactivation?: boolean;
  pendingDeactivationTeam?: string | null;
  pendingDeactivationRole?: string | null;
}

export interface SalesmanLookup {
  id: number;
  salesmancode: string;
  salesmanname: string;
  status: number;
  activeTeamId?: number | null;
  activeTeamCode?: string | null;
  isLeaderInActiveTeam?: boolean;
}

export interface SalesmanRequest {
  salesmancode: string;
  salesmanname: string;
  mobileno?: string;
  email?: string;
  joiningdate?: string;
  status: number;
}

export interface ActiveTeamAssignment {
  teamId: number;
  teamCode: string;
  teamName: string;
  roleInTeam: string;
  role?: string;
  isLeader?: boolean;
  isTeamLeader?: boolean;
}

export interface SalesmanDeactivationRequest {
  id: number;
  salesmanId: number;
  salesmanCode: string;
  salesmanName: string;
  teamId?: number | null;
  teamCode?: string | null;
  teamName?: string | null;
  roleInTeam?: string | null;
  role?: string | null;
  isLeader?: boolean;
  reason?: string | null;
  requestedBy: string;
  requestedDate: string;
  status: string;
  processedDate?: string | null;
  processedBy?: string | null;
  notes?: string | null;
}

export interface DeactivateSalesmanResult {
  directDeactivation?: boolean;
  requestSentToMarketingMaster?: boolean;
  requiresApproval?: boolean;
  immediateDeactivation?: boolean;
  message: string;
  requestId?: number | null;
  salesman?: Salesman | null;
  deactivationRequest?: SalesmanDeactivationRequest | null;
  assignment?: ActiveTeamAssignment | null;
}
