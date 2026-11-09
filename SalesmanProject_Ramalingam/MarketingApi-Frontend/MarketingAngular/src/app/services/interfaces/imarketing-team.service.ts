import { Observable } from 'rxjs';
import {
  PagedResult,
  SelectOption,
  Team,
  TeamListItem,
  TeamMember,
  TeamRequest,
} from '../../models';

export interface IMarketingTeamService {
  getList(search: string, status: number | null, pageNumber: number, pageSize: number): Observable<PagedResult<TeamListItem>>;
  getById(id: number): Observable<Team>;
  getRoleOptions(): Observable<SelectOption[]>;
  create(request: TeamRequest): Observable<Team>;
  update(id: number, request: TeamRequest): Observable<Team>;
  addSalesman(id: number, member: TeamMember): Observable<Team>;
  delete(id: number): Observable<void>;
  activate(id: number): Observable<unknown>;
  deactivate(id: number): Observable<unknown>;
}
