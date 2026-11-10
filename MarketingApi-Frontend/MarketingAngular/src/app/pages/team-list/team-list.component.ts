import { CommonModule } from '@angular/common';
import { Component, DestroyRef, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { GridModule, SortEventArgs, SortService } from '@syncfusion/ej2-angular-grids';
import { MarketingTeamApiService } from '../../services/marketing-team-api.service';
import { SalesmanApiService } from '../../services/salesman-api.service';
import { ToastService } from '../../services/toast.service';
import {
  PagedResult,
  RecordStatus,
  SalesmanDeactivationRequest,
  Team,
  TeamListItem,
} from '../../models';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal.component';
import { TeamFormComponent } from '../team-form/team-form.component';
import { SortDirection, TeamRoleType } from '../../shared/constants';
import { readApiError } from '../../shared/api-error';

@Component({
  selector: 'app-team-list',
  standalone: true,
  imports: [CommonModule, FormsModule, GridModule, ConfirmModalComponent, TeamFormComponent],
  providers: [SortService],
  templateUrl: './team-list.component.html',
  styleUrl: './team-list.component.css',
})
export class TeamListComponent implements OnInit {
  private readonly api = inject(MarketingTeamApiService);
  private readonly salesmenApi = inject(SalesmanApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly result = signal<PagedResult<TeamListItem>>({
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 15,
    totalPages: 0,
  });

  readonly search = signal('');
  readonly status = signal<number | null>(RecordStatus.Active);
  readonly totalActiveCount = signal(0);
  readonly totalInactiveCount = signal(0);
  readonly pageSize = 15;
  readonly loading = signal(false);
  readonly loadingMore = signal(false);
  readonly sortBy = signal('teamcode');
  readonly sortDir = signal<SortDirection>('asc');
  private currentPage = 1;

  // View Modal State
  readonly isViewModalOpen = signal(false);
  readonly viewingTeam = signal<Team | null>(null);
  readonly viewingHeader = signal<TeamListItem | null>(null);
  readonly loadingView = signal(false);

  readonly totalTargetInView = computed(() => {
    const t = this.viewingTeam();
    if (!t) return 0;
    return t.totalTargetAmount ?? (t.members || []).reduce((acc, m) => acc + (Number(m.targetamount) || 0), 0);
  });

  // Pending Deactivation Requests State
  readonly pendingRequests = signal<SalesmanDeactivationRequest[]>([]);
  readonly isPendingModalOpen = signal(false);
  readonly loadingPendingRequests = signal(false);
  readonly processingRequestId = signal<number | null>(null);

  // Reject Reason Prompt State (H1)
  readonly rejectingRequest = signal<SalesmanDeactivationRequest | null>(null);
  readonly rejectNotes = signal('');

  // Create / Edit Modal State (delegated to TeamFormComponent)
  readonly isEditModalOpen = signal(false);
  readonly editingTeamId = signal<number | null>(null);

  // Confirmation Modal
  readonly isModalOpen = signal(false);
  readonly modalTitle = signal('');
  readonly modalMessage = signal('');
  readonly modalSubMessage = signal('');
  readonly modalConfirmText = signal('');
  readonly modalIconType = signal<'danger' | 'warning' | 'info'>('danger');
  readonly modalConfirmBtnClass = signal('btn-danger');
  readonly modalProcessing = signal(false);
  private pendingAction: (() => void) | null = null;

  ngOnInit(): void {
    const url = this.router.url.toLowerCase();
    this.status.set(
      url.includes('/deactivated') || url.includes('/deactive') || url.includes('/inactive')
        ? RecordStatus.Inactive
        : RecordStatus.Active
    );

    this.load();
    this.loadCounts();
    this.loadPendingRequests();
  }

  loadCounts(): void {
    this.api
      .getList('', RecordStatus.Active, 1, 1)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (res) => this.totalActiveCount.set(res.totalCount) });

    this.api
      .getList('', RecordStatus.Inactive, 1, 1)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (res) => this.totalInactiveCount.set(res.totalCount) });
  }

  switchViewTab(tabStatus: 1 | 2): void {
    if (this.status() === tabStatus) return;
    this.status.set(tabStatus);
    this.currentPage = 1;
    this.result.update((r) => ({ ...r, items: [] }));
    const targetUrl = tabStatus === RecordStatus.Inactive ? '/marketing-team/deactivated' : '/marketing-team';
    this.router.navigateByUrl(targetUrl);
    this.load();
  }

  loadPendingRequests(): void {
    this.loadingPendingRequests.set(true);
    this.salesmenApi
      .getPendingDeactivationRequests()
      .pipe(
        finalize(() => this.loadingPendingRequests.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (requests) => this.pendingRequests.set(requests || []),
        error: (err) => this.toast.error(readApiError(err), 'Failed to load deactivation requests'),
      });
  }

  openPendingModal(): void {
    this.isPendingModalOpen.set(true);
    this.rejectingRequest.set(null);
    this.rejectNotes.set('');
    this.loadPendingRequests();
  }

  closePendingModal(): void {
    this.isPendingModalOpen.set(false);
    this.rejectingRequest.set(null);
    this.rejectNotes.set('');
  }

  isLeader(req: SalesmanDeactivationRequest): boolean {
    if (req.isLeader !== undefined && req.isLeader !== null) return req.isLeader;
    const role = (req.roleInTeam || req.role || '').toLowerCase();
    return role === TeamRoleType.Leader.toLowerCase();
  }

  getRoleDisplay(req: SalesmanDeactivationRequest): string {
    return this.isLeader(req) ? 'Team Leader' : 'Team Member';
  }

  approveRequest(req: SalesmanDeactivationRequest): void {
    this.processingRequestId.set(req.id);
    this.salesmenApi
      .approveDeactivation(req.id)
      .pipe(
        finalize(() => this.processingRequestId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toast.success(`Deactivation request for ${req.salesmanCode} approved.`);
          this.loadPendingRequests();
          this.load();
          this.loadCounts();
        },
        error: (err) => this.toast.error(readApiError(err), 'Approval Failed'),
      });
  }

  startRejectRequest(req: SalesmanDeactivationRequest): void {
    this.rejectingRequest.set(req);
    this.rejectNotes.set('');
  }

  cancelRejectRequest(): void {
    this.rejectingRequest.set(null);
    this.rejectNotes.set('');
  }

  confirmRejectRequest(req: SalesmanDeactivationRequest): void {
    const notes = this.rejectNotes().trim();
    if (!notes) {
      this.toast.warning('Please enter a reason for rejecting this deactivation request.');
      return;
    }

    this.processingRequestId.set(req.id);
    this.salesmenApi
      .rejectDeactivation(req.id, notes)
      .pipe(
        finalize(() => this.processingRequestId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toast.info(`Deactivation request for ${req.salesmanCode} rejected.`);
          this.cancelRejectRequest();
          this.loadPendingRequests();
        },
        error: (err) => this.toast.error(readApiError(err), 'Rejection Failed'),
      });
  }

  load(): void {
    this.currentPage = 1;
    this.loading.set(true);

    this.api
      .getList(this.search(), this.status(), 1, this.pageSize, this.sortBy(), this.sortDir())
      .pipe(
        timeout(8000),
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => this.result.set(res),
        error: (error) => this.toast.error(readApiError(error), 'Failed to load teams'),
      });
  }

  loadMore(): void {
    const current = this.result();
    if (this.loading() || this.loadingMore() || this.currentPage >= current.totalPages) {
      return;
    }

    this.loadingMore.set(true);
    const nextPage = this.currentPage + 1;

    this.api
      .getList(this.search(), this.status(), nextPage, this.pageSize, this.sortBy(), this.sortDir())
      .pipe(
        finalize(() => this.loadingMore.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => {
          this.currentPage = nextPage;
          this.result.update((prev) => ({
            ...res,
            items: [...prev.items, ...res.items],
          }));
        },
        error: (err) => this.toast.error(readApiError(err), 'Failed to load more'),
      });
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    const threshold = 250;
    const position = window.innerHeight + window.scrollY;
    const height = document.documentElement.scrollHeight;
    if (height - position < threshold) {
      this.loadMore();
    }
  }

  searchTeams(): void {
    this.currentPage = 1;
    this.result.update((r) => ({ ...r, items: [] }));
    this.load();
  }

  onGridActionBegin(args: SortEventArgs): void {
    if (args.requestType !== 'sorting' || !args.columnName) {
      return;
    }

    args.cancel = true;
    const col = args.columnName.toLowerCase();
    const dir: SortDirection = args.direction === 'Descending' ? 'desc' : 'asc';
    this.sortBy.set(col);
    this.sortDir.set(dir);
    this.load();
  }

  openViewModal(teamItem: TeamListItem): void {
    this.viewingHeader.set(teamItem);
    this.isViewModalOpen.set(true);
    this.loadingView.set(true);
    this.viewingTeam.set(null);

    this.api
      .getById(teamItem.id)
      .pipe(
        finalize(() => this.loadingView.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (team) => this.viewingTeam.set(team),
        error: (err) => {
          this.toast.error(readApiError(err), 'Cannot Load Team');
          this.closeViewModal();
        },
      });
  }

  closeViewModal(): void {
    this.isViewModalOpen.set(false);
    this.viewingTeam.set(null);
    this.viewingHeader.set(null);
  }

  openCreateModal(): void {
    this.editingTeamId.set(null);
    this.isEditModalOpen.set(true);
  }

  openEditModal(teamItem: TeamListItem | Team): void {
    this.editingTeamId.set(teamItem.id);
    this.isEditModalOpen.set(true);
  }

  closeEditModal(): void {
    this.isEditModalOpen.set(false);
    this.editingTeamId.set(null);
  }

  onFormSaved(): void {
    this.closeEditModal();
    this.load();
    this.loadCounts();
  }

  confirmStatusToggle(team: TeamListItem): void {
    const isActivating = team.status === RecordStatus.Inactive;
    this.modalTitle.set(isActivating ? 'Activate Marketing Team' : 'Deactivate Marketing Team');
    this.modalMessage.set(
      isActivating
        ? `Activate team ${team.teamcode} (${team.teamname})?`
        : `Deactivate team ${team.teamcode} (${team.teamname})?`
    );
    this.modalSubMessage.set(
      isActivating
        ? 'Team header will be set to Active if its Team Leader is active and unassigned. Member rows remain inactive until edited.'
        : 'Deactivating this team will also deactivate all its member detail rows.'
    );
    this.modalConfirmText.set(isActivating ? 'Activate' : 'Deactivate');
    this.modalIconType.set(isActivating ? 'info' : 'warning');
    this.modalConfirmBtnClass.set(isActivating ? 'btn-success' : 'btn-warning');

    this.pendingAction = () => {
      this.modalProcessing.set(true);
      const request = isActivating ? this.api.activate(team.id) : this.api.deactivate(team.id);
      request
        .pipe(
          finalize(() => {
            this.modalProcessing.set(false);
            this.isModalOpen.set(false);
          }),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe({
          next: () => {
            this.toast.success(`Team ${team.teamcode} is now ${isActivating ? 'Active' : 'Inactive'}.`);
            this.load();
            this.loadCounts();
          },
          error: (error) => this.toast.error(readApiError(error)),
        });
    };

    this.isModalOpen.set(true);
  }

  onModalConfirm(): void {
    this.pendingAction?.();
  }

  onModalCancel(): void {
    this.isModalOpen.set(false);
    this.pendingAction = null;
  }
}
