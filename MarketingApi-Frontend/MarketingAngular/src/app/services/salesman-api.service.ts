import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ActiveTeamAssignment,
  DeactivateSalesmanResult,
  PagedResult,
  Salesman,
  SalesmanDeactivationRequest,
  SalesmanLookup,
  SalesmanRequest,
} from '../models';
import { SortDirection } from '../shared/constants';

@Injectable({ providedIn: 'root' })
export class SalesmanApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/SalesmanMaster`;

  getList(
    search: string,
    status: number | null,
    pageNumber: number,
    pageSize: number,
    sortBy = 'salesmancode',
    sortDir: SortDirection = 'asc'
  ): Observable<PagedResult<Salesman>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('sortBy', sortBy)
      .set('sortDir', sortDir);

    if (search.trim()) params = params.set('search', search.trim());
    if (status !== null) params = params.set('status', status);

    return this.http.get<PagedResult<Salesman>>(this.url, { params });
  }

  getById(id: number): Observable<Salesman> {
    return this.http.get<Salesman>(`${this.url}/${id}`);
  }

  getActiveLookup(): Observable<SalesmanLookup[]> {
    return this.http.get<SalesmanLookup[]>(`${this.url}/lookup`);
  }

  getTeamAssignment(id: number): Observable<ActiveTeamAssignment | null> {
    return this.http.get<ActiveTeamAssignment | null>(`${this.url}/${id}/team-assignment`);
  }

  create(request: SalesmanRequest): Observable<Salesman> {
    return this.http.post<Salesman>(this.url, request);
  }

  update(id: number, request: Omit<SalesmanRequest, 'salesmancode' | 'status'>): Observable<Salesman> {
    return this.http.put<Salesman>(`${this.url}/${id}`, request);
  }

  activate(id: number): Observable<Salesman> {
    return this.http.put<Salesman>(`${this.url}/${id}/activate`, {});
  }

  deactivate(id: number): Observable<DeactivateSalesmanResult> {
    return this.http.put<DeactivateSalesmanResult>(`${this.url}/${id}/deactivate`, {});
  }

  getPendingDeactivationRequests(): Observable<SalesmanDeactivationRequest[]> {
    return this.http.get<SalesmanDeactivationRequest[]>(`${this.url}/deactivation-requests/pending`);
  }

  approveDeactivation(id: number, notes?: string): Observable<SalesmanDeactivationRequest> {
    return this.http.post<SalesmanDeactivationRequest>(`${this.url}/deactivation-requests/${id}/approve`, {
      notes: notes || '',
    });
  }

  rejectDeactivation(id: number, notes?: string): Observable<SalesmanDeactivationRequest> {
    return this.http.post<SalesmanDeactivationRequest>(`${this.url}/deactivation-requests/${id}/reject`, {
      notes: notes || '',
    });
  }
}
