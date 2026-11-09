export enum SalesmanRole {
  TeamLeader = 1,
  SeniorSalesExecutive = 2,
  SalesExecutive = 3,
  Trainee = 4,
}

export interface TeamMember {
  id?: number;
  salesmanid: number;
  salesmancode?: string;
  salesmanname?: string;
  role: number;
  roleName?: string;
  targetamount: number;
  status: number;
  statusName?: string;
}

export interface Team {
  id: number;
  teamcode: string;
  teamname: string;
  teamleaderid: number;
  teamleadername: string;
  description?: string;
  status: number;
  statusName?: string;
  createddate?: string;
  modifieddate?: string;
  members: TeamMember[];
  totalTargetAmount?: number;
}

export interface TeamListItem {
  id: number;
  teamcode: string;
  teamname: string;
  teamleadername: string;
  salesmancount: number;
  status: number;
  statusName?: string;
}

export interface TeamRequest {
  teamcode?: string;
  teamname: string;
  teamleaderid: number;
  description?: string;
  status?: number;
  members: TeamMember[];
}
