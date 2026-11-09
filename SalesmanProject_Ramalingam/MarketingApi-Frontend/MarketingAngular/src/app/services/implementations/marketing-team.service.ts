import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  PagedResult,
  SelectOption,
  Team,
  TeamListItem,
  TeamMember,
  TeamRequest,
} from '../../models';
import { IMarketingTeamService } from '../interfaces/imarketing-team.service';
import { MarketingTeamRepository } from '../../repositories/implementations/marketing-team.repository';

@Injectable({ providedIn: 'root' })
export class MarketingTeamService implements IMarketingTeamService {
  private readonly repository = inject(MarketingTeamRepository);

  getList(search: string, status: number | null, pageNumber: number, pageSize: number): Observable<PagedResult<TeamListItem>> {
    return this.repository.getList(search, status, pageNumber, pageSize);
  }

  getById(id: number): Observable<Team> {
    return this.repository.getById(id);
  }

  getRoleOptions(): Observable<SelectOption[]> {
    return this.repository.getRoleOptions();
  }

  create(request: TeamRequest): Observable<Team> {
    return this.repository.create(request);
  }

  update(id: number, request: TeamRequest): Observable<Team> {
    return this.repository.update(id, request);
  }

  addSalesman(id: number, member: TeamMember): Observable<Team> {
    return this.repository.addSalesman(id, member);
  }

  delete(id: number): Observable<void> {
    return this.repository.delete(id);
  }

  activate(id: number): Observable<unknown> {
    return this.repository.activate(id);
  }

  deactivate(id: number): Observable<unknown> {
    return this.repository.deactivate(id);
  }
}
