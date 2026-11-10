import { CommonModule } from '@angular/common';
import { Component, DestroyRef, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, catchError, debounceTime, finalize, of, switchMap } from 'rxjs';
import { GridModule, SortEventArgs, SortService } from '@syncfusion/ej2-angular-grids';
import { SalesmanApiService } from '../../services/salesman-api.service';
import { ToastService } from '../../services/toast.service';
import { PagedResult, RecordStatus, Salesman } from '../../models';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal.component';
import { SalesmanFormComponent } from '../salesman-form/salesman-form.component';
import { SortDirection } from '../../shared/constants';
import { readApiError } from '../../shared/api-error';

@Component({
  selector: 'app-salesman-list',
  standalone: true,
  imports: [CommonModule, FormsModule, GridModule, ConfirmModalComponent, SalesmanFormComponent],
  providers: [SortService],
  templateUrl: './salesman-list.component.html',
})
export class SalesmanListComponent implements OnInit {
  private readonly api = inject(SalesmanApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly searchSubject = new Subject<void>();

  readonly result = signal<PagedResult<Salesman>>({
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
  readonly sortBy = signal('salesmancode');
  readonly sortDir = signal<SortDirection>('asc');
  private currentPage = 1;

  readonly activeCount = computed(
    () => this.result().items.filter((s) => s.status === RecordStatus.Active).length
  );
  readonly inactiveCount = computed(
    () => this.result().items.filter((s) => s.status === RecordStatus.Inactive).length
  );

  // View Modal State
  readonly isViewModalOpen = signal(false);
  readonly viewingSalesman = signal<Salesman | null>(null);

  // Create / Edit Modal State (delegated to SalesmanFormComponent)
  readonly isEditModalOpen = signal(false);
  readonly editingSalesman = signal<Salesman | null>(null);

  // Toggle Confirmation Modal State
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
    const currentUrl = this.router.url.toLowerCase();
    this.status.set(
      currentUrl.includes('/deactivated') || currentUrl.includes('/deactive') || currentUrl.includes('/inactive')
        ? RecordStatus.Inactive
        : RecordStatus.Active
    );

    this.searchSubject
      .pipe(
        debounceTime(350),
        switchMap(() => {
          this.currentPage = 1;
          this.loading.set(true);
          return this.api
            .getList(this.search(), this.status(), 1, this.pageSize, this.sortBy(), this.sortDir())
            .pipe(
              catchError((error) => {
                this.toast.error(readApiError(error), 'Failed to load salesmen');
                return of(null);
              }),
              finalize(() => this.loading.set(false))
            );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((res) => {
        if (res !== null) {
          this.result.set(res);
          if (this.status() === RecordStatus.Active) this.totalActiveCount.set(res.totalCount);
          if (this.status() === RecordStatus.Inactive) this.totalInactiveCount.set(res.totalCount);
        }
      });

    this.load();
    this.loadCounts();
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
    this.router.navigate([tabStatus === RecordStatus.Active ? '/salesman' : '/salesman/deactivated']);
    this.load();
    this.loadCounts();
  }

  load(): void {
    this.currentPage = 1;
    this.searchSubject.next();
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
        error: (err) => {
          this.toast.error(readApiError(err), 'Failed to load more');
        },
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

  onSearchInput(value: string): void {
    this.search.set(value);
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

  openViewModal(salesman: Salesman): void {
    this.viewingSalesman.set(salesman);
    this.isViewModalOpen.set(true);
  }

  closeViewModal(): void {
    this.isViewModalOpen.set(false);
    this.viewingSalesman.set(null);
  }

  openCreateModal(): void {
    this.editingSalesman.set(null);
    this.isEditModalOpen.set(true);
  }

  openEditModal(salesman: Salesman): void {
    this.editingSalesman.set(salesman);
    this.isEditModalOpen.set(true);
  }

  closeEditModal(): void {
    this.isEditModalOpen.set(false);
    this.editingSalesman.set(null);
  }

  onFormSaved(): void {
    this.closeEditModal();
    this.load();
    this.loadCounts();
  }

  confirmStatusToggle(salesman: Salesman): void {
    const isActivating = salesman.status === RecordStatus.Inactive;

    if (isActivating) {
      this.modalTitle.set('Activate Salesman');
      this.modalMessage.set(`Are you sure you want to activate salesman ${salesman.salesmancode} (${salesman.salesmanname})?`);
      this.modalSubMessage.set('This salesman will now be available to be added to active marketing teams.');
      this.modalConfirmText.set('Activate');
      this.modalIconType.set('info');
      this.modalConfirmBtnClass.set('btn-success');

      this.pendingAction = () => {
        this.modalProcessing.set(true);
        this.api
          .activate(salesman.id)
          .pipe(
            finalize(() => {
              this.modalProcessing.set(false);
              this.isModalOpen.set(false);
            }),
            takeUntilDestroyed(this.destroyRef)
          )
          .subscribe({
            next: () => {
              this.toast.success(`Salesman ${salesman.salesmancode} was activated successfully.`);
              this.load();
              this.loadCounts();
            },
            error: (error) => this.toast.error(readApiError(error)),
          });
      };

      this.isModalOpen.set(true);
      return;
    }

    this.api
      .getTeamAssignment(salesman.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (assignment) => {
          if (assignment?.teamId) {
            const isLeader = assignment.isTeamLeader || assignment.role === 'Leader';
            this.modalTitle.set('Deactivation Approval Required');
            this.modalMessage.set(
              `Salesman ${salesman.salesmancode} (${salesman.salesmanname}) is assigned to active team "${assignment.teamCode} - ${assignment.teamName}" as ${isLeader ? 'Team Leader' : 'Team Member'}.`
            );
            this.modalSubMessage.set(
              isLeader
                ? 'Because this salesman is the Team Leader, a deactivation request will be sent to the Marketing Master. When approved, the marketing team and all members inside it will also be deactivated.'
                : 'Because this salesman is a Team Member, a deactivation request will be sent to the Marketing Master. When approved, this member will be deactivated in the team and the salesman deactivated.'
            );
            this.modalConfirmText.set('Submit Deactivation Request');
            this.modalIconType.set('warning');
            this.modalConfirmBtnClass.set('btn-warning');
          } else {
            this.modalTitle.set('Deactivate Salesman');
            this.modalMessage.set(`Are you sure you want to deactivate salesman ${salesman.salesmancode} (${salesman.salesmanname})?`);
            this.modalSubMessage.set('This salesman is not assigned to any active marketing team and will be deactivated immediately.');
            this.modalConfirmText.set('Deactivate');
            this.modalIconType.set('warning');
            this.modalConfirmBtnClass.set('btn-warning');
          }

          this.pendingAction = () => {
            this.modalProcessing.set(true);
            this.api
              .deactivate(salesman.id)
              .pipe(
                finalize(() => {
                  this.modalProcessing.set(false);
                  this.isModalOpen.set(false);
                }),
                takeUntilDestroyed(this.destroyRef)
              )
              .subscribe({
                next: (res) => {
                  if (res.requiresApproval || res.requestSentToMarketingMaster) {
                    this.toast.info(res.message, 'Approval Request Submitted');
                  } else {
                    this.toast.success(res.message || `Salesman ${salesman.salesmancode} deactivated successfully.`);
                  }
                  this.load();
                  this.loadCounts();
                },
                error: (error) => this.toast.error(readApiError(error)),
              });
          };

          this.isModalOpen.set(true);
        },
        error: (err) => this.toast.error(readApiError(err), 'Failed to check team assignment'),
      });
  }

  onModalConfirm(): void {
    this.pendingAction?.();
  }

  onModalCancel(): void {
    this.isModalOpen.set(false);
    this.pendingAction = null;
  }
}
