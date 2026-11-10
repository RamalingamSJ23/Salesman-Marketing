import { Observable } from 'rxjs';
import {
  ActiveTeamAssignment,
  DeactivateSalesmanResult,
  PagedResult,
  Salesman,
  SalesmanDeactivationRequest,
  SalesmanLookup,
  SalesmanRequest,
} from '../../models';

export interface ISalesmanRepository {
  getList(search: string, status: number | null, pageNumber: number, pageSize: number): Observable<PagedResult<Salesman>>;
  getById(id: number): Observable<Salesman>;
  getActiveLookup(): Observable<SalesmanLookup[]>;
  getTeamAssignment(id: number): Observable<ActiveTeamAssignment | null>;
  create(request: SalesmanRequest): Observable<Salesman>;
  update(id: number, request: Omit<SalesmanRequest, 'salesmancode' | 'status'>): Observable<Salesman>;
  delete(id: number): Observable<void>;
  activate(id: number): Observable<unknown>;
  deactivate(id: number): Observable<DeactivateSalesmanResult>;
  getPendingDeactivationRequests(): Observable<SalesmanDeactivationRequest[]>;
  approveDeactivation(id: number, notes?: string): Observable<{ message: string }>;
  rejectDeactivation(id: number, notes?: string): Observable<{ message: string }>;
}
