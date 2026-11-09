import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PagedResult,
  SelectOption,
  Team,
  TeamListItem,
  TeamMember,
  TeamRequest,
} from '../../models';
import { IMarketingTeamRepository } from '../interfaces/imarketing-team.repository';

@Injectable({ providedIn: 'root' })
export class MarketingTeamRepository implements IMarketingTeamRepository {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/MarketingTeamMaster`;

  getList(search: string, status: number | null, pageNumber: number, pageSize: number): Observable<PagedResult<TeamListItem>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    if (search.trim()) params = params.set('search', search.trim());
    if (status !== null) params = params.set('status', status);

    return this.http.get<PagedResult<TeamListItem>>(this.url, { params });
  }

  getById(id: number): Observable<Team> {
    return this.http.get<Team>(`${this.url}/${id}`);
  }

  getRoleOptions(): Observable<SelectOption[]> {
    return this.http.get<SelectOption[]>(`${this.url}/role-options`);
  }

  create(request: TeamRequest): Observable<Team> {
    return this.http.post<Team>(this.url, request);
  }

  update(id: number, request: TeamRequest): Observable<Team> {
    return this.http.put<Team>(`${this.url}/${id}`, request);
  }

  addSalesman(id: number, member: TeamMember): Observable<Team> {
    return this.http.post<Team>(`${this.url}/${id}/salesmen`, member);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  activate(id: number): Observable<unknown> {
    return this.http.put(`${this.url}/${id}/activate`, {});
  }

  deactivate(id: number): Observable<unknown> {
    return this.http.put(`${this.url}/${id}/deactivate`, {});
  }
}
