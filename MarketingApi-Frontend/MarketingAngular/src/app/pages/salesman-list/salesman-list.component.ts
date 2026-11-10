import { CommonModule } from '@angular/common';
import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef, HostListener } from '@angular/core';
import { AbstractControl, FormBuilder, FormsModule, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, debounceTime, switchMap, takeUntil, finalize, catchError, of } from 'rxjs';
import { GridModule, SortService } from '@syncfusion/ej2-angular-grids';

import { SalesmanApiService } from '../../services/salesman-api.service';
import { ToastService } from '../../services/toast.service';
import { PagedResult, Salesman } from '../../shared/models';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal.component';

function noFutureDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const selected = new Date(control.value);
  if (isNaN(selected.getTime())) return null;
  const today = new Date();
  today.setHours(23, 59, 59, 999);
  if (selected > today) {
    return { futureDate: true };
  }
  return null;
}

@Component({
  selector: 'app-salesman-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, GridModule, ConfirmModalComponent],
  providers: [SortService],
  templateUrl: './salesman-list.component.html',
})
export class SalesmanListComponent implements OnInit, OnDestroy {
  private readonly api = inject(SalesmanApiService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly searchSubject = new Subject<void>();
  private readonly destroy$ = new Subject<void>();

  result: PagedResult<Salesman> = {
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 15,
    totalPages: 0,
  };

  search = '';
  status: number | null = 1; // Default to Active only
  totalActiveCount = 0;
  totalInactiveCount = 0;
  pageNumber = 1;
  pageSize = 15;
  loading = false;
  loadingMore = false;
  sortColumn: 'salesmancode' | 'salesmanname' = 'salesmancode';
  sortAsc = true;

  get activeCount(): number {
    return this.result.items.filter((s) => s.status === 1).length;
  }

  get inactiveCount(): number {
    return this.result.items.filter((s) => s.status === 2).length;
  }

  // View Modal State
  isViewModalOpen = false;
  viewingSalesman: Salesman | null = null;

  // Create / Edit Modal State
  isEditModalOpen = false;
  isEditMode = false;
  editingId: number | null = null;
  savingSalesman = false;
  formError = '';
  codeConflictError = '';
  mobileConflictError = '';
  emailConflictError = '';

  readonly form = this.fb.nonNullable.group({
    salesmancode: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(20),
        Validators.pattern(/^[a-zA-Z0-9]+$/),
      ],
    ],
    salesmanname: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
        Validators.pattern(/^[a-zA-Z\s.'-]+$/),
      ],
    ],
    mobileno: [
      '',
      [
        Validators.required,
        Validators.pattern(/^[0-9]{7,15}$/),
      ],
    ],
    email: [
      '',
      [
        Validators.required,
        Validators.maxLength(100),
        Validators.pattern(/^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/),
      ],
    ],
    joiningdate: ['', [Validators.required, noFutureDateValidator]],
    status: [1, Validators.required],
  });

  // Toggle Confirmation Modal State
  isModalOpen = false;
  modalTitle = '';
  modalMessage = '';
  modalSubMessage = '';
  modalConfirmText = '';
  modalIconType: 'danger' | 'warning' | 'info' = 'danger';
  modalConfirmBtnClass = 'btn-danger';
  modalProcessing = false;
  private pendingAction: (() => void) | null = null;

  ngOnInit(): void {
    const currentUrl = this.router.url;
    if (currentUrl.includes('/deactive') || currentUrl.includes('/inactive')) {
      this.status = 2;
    } else {
      this.status = 1;
    }

    this.searchSubject
      .pipe(
        debounceTime(350),
        switchMap(() => {
          this.loading = true;
          this.cdr.markForCheck();
          return this.api
            .getList(this.search, this.status, this.pageNumber, this.pageSize)
            .pipe(
              catchError((error) => {
                this.toast.error(this.readError(error), 'Failed to load salesmen');
                return of(null);
              }),
              finalize(() => {
                this.loading = false;
                this.cdr.markForCheck();
              })
            );
        }),
        takeUntil(this.destroy$)
      )
      .subscribe((result) => {
        if (result !== null) {
          this.result = result;
          this.applySort();
          if (this.status === 1) this.totalActiveCount = result.totalCount;
          if (this.status === 2) this.totalInactiveCount = result.totalCount;
        }
        this.cdr.markForCheck();
      });

    this.load();
    this.loadCounts();
  }

  loadCounts(): void {
    this.api.getList('', 1, 1, 1).subscribe({
      next: (res) => {
        this.totalActiveCount = res.totalCount;
        this.cdr.markForCheck();
      },
    });
    this.api.getList('', 2, 1, 1).subscribe({
      next: (res) => {
        this.totalInactiveCount = res.totalCount;
        this.cdr.markForCheck();
      },
    });
  }

  switchViewTab(tabStatus: 1 | 2): void {
    if (this.status === tabStatus) return;
    this.status = tabStatus;
    this.pageNumber = 1;
    this.result.items = [];
    this.router.navigate([tabStatus === 1 ? '/salesman' : '/salesman/deactive']);
    this.load();
    this.loadCounts();
  }

  load(): void {
    this.searchSubject.next();
  }

  loadMore(): void {
    if (this.loading || this.loadingMore || this.pageNumber >= this.result.totalPages) {
      return;
    }
    this.loadingMore = true;
    this.pageNumber++;
    this.cdr.markForCheck();

    this.api
      .getList(this.search, this.status, this.pageNumber, this.pageSize)
      .pipe(
        finalize(() => {
          this.loadingMore = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (res) => {
          this.result = {
            ...res,
            items: [...this.result.items, ...res.items],
          };
          this.applySort();
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.toast.error(this.readError(err), 'Failed to load more');
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

  onTableScroll(event: Event): void {
    const el = event.target as HTMLElement;
    if (el.scrollHeight - el.scrollTop - el.clientHeight < 150) {
      this.loadMore();
    }
  }

  onSearchChange(): void {
    this.pageNumber = 1;
    this.result.items = [];
    this.load();
  }

  clearSearch(): void {
    this.search = '';
    this.onSearchChange();
  }

  onStatusChange(val: string): void {
    this.status = val === '' ? null : Number(val);
    this.pageNumber = 1;
    this.result.items = [];
    this.load();
  }

  onStatusFilterChange(status: number | null): void {
    this.status = status;
    this.pageNumber = 1;
    this.result.items = [];
    this.load();
  }

  toggleSort(col: 'salesmancode' | 'salesmanname'): void {
    if (this.sortColumn === col) {
      this.sortAsc = !this.sortAsc;
    } else {
      this.sortColumn = col;
      this.sortAsc = true;
    }
    this.applySort();
    this.cdr.markForCheck();
  }

  private applySort(): void {
    if (!this.result.items || this.result.items.length === 0) return;
    this.result.items = [...this.result.items].sort((a, b) => {
      const valA = (a[this.sortColumn] || '').toString().toLowerCase();
      const valB = (b[this.sortColumn] || '').toString().toLowerCase();
      return this.sortAsc ? valA.localeCompare(valB) : valB.localeCompare(valA);
    });
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.pageNumber = 1;
    this.load();
  }

  changePage(page: number): void {
    if (page < 1 || page > this.result.totalPages || page === this.pageNumber) {
      return;
    }
    this.pageNumber = page;
    this.load();
  }

  // --- View Modal ---
  openViewModal(salesman: Salesman): void {
    this.viewingSalesman = salesman;
    this.isViewModalOpen = true;
    this.cdr.markForCheck();
  }

  closeViewModal(): void {
    this.isViewModalOpen = false;
    this.viewingSalesman = null;
    this.cdr.markForCheck();
  }

  // --- Create / Edit Modal ---
  openCreateModal(): void {
    this.isEditMode = false;
    this.editingId = null;
    this.formError = '';
    this.codeConflictError = '';
    this.mobileConflictError = '';
    this.emailConflictError = '';
    this.form.reset({
      salesmancode: '',
      salesmanname: '',
      mobileno: '',
      email: '',
      joiningdate: '',
      status: 1,
    });
    this.isEditModalOpen = true;
    this.cdr.markForCheck();
  }

  openEditModal(salesman: Salesman): void {
    this.isEditMode = true;
    this.editingId = salesman.id;
    this.formError = '';
    this.codeConflictError = '';
    this.mobileConflictError = '';
    this.emailConflictError = '';
    this.form.patchValue({
      salesmancode: salesman.salesmancode,
      salesmanname: salesman.salesmanname,
      mobileno: salesman.mobileno || '',
      email: salesman.email || '',
      joiningdate: salesman.joiningdate ? salesman.joiningdate.slice(0, 10) : '',
      status: salesman.status,
    });
    this.isEditModalOpen = true;
    this.cdr.markForCheck();
  }

  closeEditModal(): void {
    this.isEditModalOpen = false;
    this.editingId = null;
    this.cdr.markForCheck();
  }

  onCodeInput(): void {
    const current = this.form.controls.salesmancode.value;
    if (current) {
      this.form.controls.salesmancode.setValue(current.toUpperCase(), { emitEvent: false });
    }
    this.codeConflictError = '';
  }

  onMobileInput(): void {
    this.mobileConflictError = '';
  }

  onEmailInput(): void {
    this.emailConflictError = '';
  }

  getErrorMessage(field: string): string {
    const control = this.form.get(field);
    if (!control || !control.errors || !control.touched) return '';

    if (control.hasError('required')) {
      switch (field) {
        case 'salesmancode': return 'Salesman code is required.';
        case 'salesmanname': return 'Salesman name is required.';
        case 'mobileno': return 'Mobile number is required.';
        case 'email': return 'Email is required.';
        case 'joiningdate': return 'Joining date is required.';
        case 'status': return 'Status is required.';
        default: return 'This field is required.';
      }
    }
    if (control.hasError('minlength')) {
      return `Must be at least ${control.errors['minlength'].requiredLength} characters.`;
    }
    if (control.hasError('maxlength')) {
      return `Cannot exceed ${control.errors['maxlength'].requiredLength} characters.`;
    }
    if (control.hasError('pattern')) {
      switch (field) {
        case 'salesmancode': return 'Only alphanumeric characters (A-Z, 0-9) allowed.';
        case 'salesmanname': return 'Only English letters (A-Z, a-z), spaces, and standard characters (., -, \') are allowed.';
        case 'mobileno': return 'Mobile number must be 7 to 15 digits.';
        case 'email':
          if (control.value && /[A-Z]/.test(control.value)) {
            return 'Capital letters are not allowed in email. Use lowercase only.';
          }
          return 'Enter a valid email address in lowercase (e.g. name@company.com).';
        default: return 'Invalid format.';
      }
    }
    if (control.hasError('futureDate')) {
      return 'Joining date cannot be in the future.';
    }
    return 'Invalid value.';
  }

  isFieldInvalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && control.touched;
  }

  saveSalesman(saveAndNew = false): void {
    this.codeConflictError = '';
    this.mobileConflictError = '';
    this.emailConflictError = '';
    this.formError = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please fix validation errors.');
      this.cdr.markForCheck();
      return;
    }

    const value = this.form.getRawValue();

    if (value.email && /[A-Z]/.test(value.email)) {
      this.emailConflictError = 'Capital letters are not allowed in email. Use lowercase only.';
      this.toast.error(this.emailConflictError, 'Validation Error');
      this.cdr.markForCheck();
      return;
    }

    this.savingSalesman = true;
    this.cdr.markForCheck();

    const request = this.isEditMode
      ? this.api.update(this.editingId!, {
          salesmanname: value.salesmanname.trim(),
          mobileno: value.mobileno?.trim() || undefined,
          email: value.email?.trim() || undefined,
          joiningdate: value.joiningdate || undefined,
        })
      : this.api.create({
          salesmancode: value.salesmancode.trim().toUpperCase(),
          salesmanname: value.salesmanname.trim(),
          mobileno: value.mobileno?.trim() || undefined,
          email: value.email?.trim() || undefined,
          joiningdate: value.joiningdate || undefined,
          status: Number(value.status),
        });

    request.subscribe({
      next: (saved) => {
        this.savingSalesman = false;
        this.toast.success(
          `Salesman ${saved.salesmancode} ${this.isEditMode ? 'updated' : 'created'} successfully.`
        );
        this.load();

        if (saveAndNew && !this.isEditMode) {
          this.form.reset({
            salesmancode: '',
            salesmanname: '',
            mobileno: '',
            email: '',
            joiningdate: '',
            status: 1,
          });
          this.codeConflictError = '';
          this.mobileConflictError = '';
          this.emailConflictError = '';
          this.cdr.markForCheck();
        } else {
          this.closeEditModal();
        }
      },
      error: (error) => {
        this.savingSalesman = false;
        const msg = this.readError(error);
        const lower = msg.toLowerCase();
        if (lower.includes('mobile')) {
          this.mobileConflictError = msg;
          this.toast.error(msg, 'Mobile Conflict');
        } else if (lower.includes('email')) {
          this.emailConflictError = msg;
          this.toast.error(msg, 'Email Conflict');
        } else if (error.status === 409 || lower.includes('code') || lower.includes('already exists')) {
          this.codeConflictError = msg || `Salesman code ${value.salesmancode} already exists.`;
          this.toast.error(this.codeConflictError, 'Code Conflict');
        } else {
          this.formError = msg;
          this.toast.error(msg, 'Save Failed');
        }
        this.cdr.markForCheck();
      },
    });
  }

  // Activate / Deactivate Salesman
  confirmStatusToggle(salesman: Salesman): void {
    this.confirmStatusChange(salesman);
  }

  confirmStatusChange(salesman: Salesman): void {
    const isActivating = salesman.status === 2;

    if (isActivating) {
      this.modalTitle = 'Activate Salesman';
      this.modalMessage = `Are you sure you want to activate salesman ${salesman.salesmancode} (${salesman.salesmanname})?`;
      this.modalSubMessage = 'This salesman will now be available to be added to active marketing teams.';
      this.modalConfirmText = 'Activate';
      this.modalIconType = 'info';
      this.modalConfirmBtnClass = 'btn-success';

      this.pendingAction = () => {
        this.modalProcessing = true;
        this.cdr.markForCheck();
        this.api.activate(salesman.id).subscribe({
          next: () => {
            this.modalProcessing = false;
            this.isModalOpen = false;
            this.toast.success(`Salesman ${salesman.salesmancode} was activated successfully.`);
            this.cdr.markForCheck();
            this.load();
          },
          error: (error) => {
            this.modalProcessing = false;
            this.isModalOpen = false;
            this.toast.error(this.readError(error));
            this.cdr.markForCheck();
          },
        });
      };

      this.isModalOpen = true;
      this.cdr.markForCheck();
      return;
    }

    // Deactivating: check active team assignment first
    this.api.getTeamAssignment(salesman.id).subscribe({
      next: (assignment) => {
        if (assignment && assignment.teamId) {
          const isLeader = assignment.isTeamLeader || assignment.role === 'TeamLeader';
          this.modalTitle = 'Deactivation Approval Required';
          this.modalMessage = `Salesman ${salesman.salesmancode} (${salesman.salesmanname}) is assigned to active team "${assignment.teamCode} - ${assignment.teamName}" as ${isLeader ? 'Team Leader' : 'Team Member'}.`;
          this.modalSubMessage = isLeader
            ? 'Because this salesman is the Team Leader, a deactivation request will be sent to the Marketing Master. When approved, the marketing team and all members inside it will also be deactivated.'
            : 'Because this salesman is a Team Member, a deactivation request will be sent to the Marketing Master. When approved, this member will be deactivated in the team and the salesman deactivated.';
          this.modalConfirmText = 'Submit Deactivation Request';
          this.modalIconType = 'warning';
          this.modalConfirmBtnClass = 'btn-warning';
        } else {
          this.modalTitle = 'Deactivate Salesman';
          this.modalMessage = `Are you sure you want to deactivate salesman ${salesman.salesmancode} (${salesman.salesmanname})?`;
          this.modalSubMessage = 'This salesman is not assigned to any active marketing team and will be deactivated immediately.';
          this.modalConfirmText = 'Deactivate';
          this.modalIconType = 'warning';
          this.modalConfirmBtnClass = 'btn-warning';
        }

        this.pendingAction = () => {
          this.modalProcessing = true;
          this.cdr.markForCheck();
          this.api.deactivate(salesman.id).subscribe({
            next: (res) => {
              this.modalProcessing = false;
              this.isModalOpen = false;
              if (res.requiresApproval) {
                this.toast.info(res.message, 'Approval Request Submitted');
              } else {
                this.toast.success(res.message || `Salesman ${salesman.salesmancode} deactivated successfully.`);
              }
              this.cdr.markForCheck();
              this.load();
            },
            error: (error) => {
              this.modalProcessing = false;
              this.isModalOpen = false;
              this.toast.error(this.readError(error));
              this.cdr.markForCheck();
            },
          });
        };

        this.isModalOpen = true;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.toast.error(this.readError(err), 'Failed to check team assignment');
      },
    });
  }

  // Delete Salesman (Protected by Rule R2)
  confirmDelete(salesman: Salesman): void {
    this.modalTitle = 'Delete Salesman Record';
    this.modalMessage = `Are you sure you want to permanently delete ${salesman.salesmancode} (${salesman.salesmanname})?`;
    this.modalSubMessage = 'Note: Salesmen assigned to any marketing team cannot be deleted (Rule R2).';
    this.modalConfirmText = 'Delete Salesman';
    this.modalIconType = 'danger';
    this.modalConfirmBtnClass = 'btn-danger';

    this.pendingAction = () => {
      this.modalProcessing = true;
      this.cdr.markForCheck();
      this.api.delete(salesman.id).subscribe({
        next: () => {
          this.modalProcessing = false;
          this.isModalOpen = false;
          this.toast.success(`Salesman ${salesman.salesmancode} deleted successfully.`);
          this.cdr.markForCheck();
          this.load();
        },
        error: (error) => {
          this.modalProcessing = false;
          this.isModalOpen = false;
          const msg = this.readError(error);
          this.toast.error(msg, 'Cannot Delete Salesman');
          this.cdr.markForCheck();
        },
      });
    };

    this.isModalOpen = true;
    this.cdr.markForCheck();
  }

  onModalConfirm(): void {
    if (this.pendingAction) {
      this.pendingAction();
    }
  }

  onModalCancel(): void {
    this.isModalOpen = false;
    this.pendingAction = null;
    this.cdr.markForCheck();
  }

  private readError(error: any): string {
    return error?.error?.message || error?.message || 'The request could not be completed.';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.searchSubject.complete();
  }
}