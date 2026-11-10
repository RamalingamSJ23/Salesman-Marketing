import { Injectable, inject } from '@angular/core';
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
import { ISalesmanService } from '../interfaces/isalesman.service';
import { SalesmanRepository } from '../../repositories/implementations/salesman.repository';

@Injectable({ providedIn: 'root' })
export class SalesmanService implements ISalesmanService {
  private readonly repository = inject(SalesmanRepository);

  getList(search: string, status: number | null, pageNumber: number, pageSize: number): Observable<PagedResult<Salesman>> {
    return this.repository.getList(search, status, pageNumber, pageSize);
  }

  getById(id: number): Observable<Salesman> {
    return this.repository.getById(id);
  }

  getActiveLookup(): Observable<SalesmanLookup[]> {
    return this.repository.getActiveLookup();
  }

  getTeamAssignment(id: number): Observable<ActiveTeamAssignment | null> {
    return this.repository.getTeamAssignment(id);
  }

  create(request: SalesmanRequest): Observable<Salesman> {
    return this.repository.create(request);
  }

  update(id: number, request: Omit<SalesmanRequest, 'salesmancode' | 'status'>): Observable<Salesman> {
    return this.repository.update(id, request);
  }

  delete(id: number): Observable<void> {
    return this.repository.delete(id);
  }

  activate(id: number): Observable<unknown> {
    return this.repository.activate(id);
  }

  deactivate(id: number): Observable<DeactivateSalesmanResult> {
    return this.repository.deactivate(id);
  }

  getPendingDeactivationRequests(): Observable<SalesmanDeactivationRequest[]> {
    return this.repository.getPendingDeactivationRequests();
  }

  approveDeactivation(id: number, notes?: string): Observable<{ message: string }> {
    return this.repository.approveDeactivation(id, notes);
  }

  rejectDeactivation(id: number, notes?: string): Observable<{ message: string }> {
    return this.repository.rejectDeactivation(id, notes);
  }
}
