import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef, HostListener } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, timeout, finalize } from 'rxjs';
import { GridModule, SortService } from '@syncfusion/ej2-angular-grids';
import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import { MarketingTeamApiService } from '../../services/marketing-team-api.service';
import { SalesmanApiService } from '../../services/salesman-api.service';
import { ToastService } from '../../services/toast.service';
import {
  PagedResult,
  RecordStatus,
  SalesmanDeactivationRequest,
  SalesmanLookup,
  SalesmanRole,
  SelectOption,
  Team,
  TeamListItem,
  TeamMember,
} from '../../shared/models';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-team-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, GridModule, DropDownListModule, ConfirmModalComponent],
  providers: [SortService],
  templateUrl: './team-list.component.html',
})
export class TeamListComponent implements OnInit {
  private readonly api = inject(MarketingTeamApiService);
  private readonly salesmenApi = inject(SalesmanApiService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  result: PagedResult<TeamListItem> = {
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 15,
    totalPages: 0,
  };

  search = '';
  status: number | null = 1;
  totalActiveCount = 0;
  totalInactiveCount = 0;
  pageNumber = 1;
  pageSize = 15;
  loading = false;
  loadingMore = false;
  sortColumn: 'teamcode' | 'teamname' = 'teamcode';
  sortAsc = true;

  get activeCount(): number {
    return this.result.items.filter((t) => t.status === 1).length;
  }

  get inactiveCount(): number {
    return this.result.items.filter((t) => t.status === 2).length;
  }

  get totalSalesmenCount(): number {
    return this.result.items.reduce((acc, t) => acc + (t.salesmancount || 0), 0);
  }

  // View Modal State
  isViewModalOpen = false;
  viewingTeam: Team | null = null;
  viewingHeader: TeamListItem | null = null;
  loadingView = false;

  // Pending Deactivation Requests State
  pendingRequests: SalesmanDeactivationRequest[] = [];
  isPendingModalOpen = false;
  loadingPendingRequests = false;
  processingRequestId: number | null = null;

  // Create / Edit Modal State
  isEditModalOpen = false;
  isEditMode = false;
  editingId: number | null = null;
  savingTeam = false;
  formError = '';
  codeConflictError = '';

  activeSalesmen: SalesmanLookup[] = [];
  roleOptions: SelectOption[] = [];
  members: TeamMember[] = [];

  readonly form = this.fb.nonNullable.group({
    teamcode: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(20),
        Validators.pattern(/^[a-zA-Z0-9]+$/),
      ],
    ],
    teamname: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
        Validators.pattern(/^[a-zA-Z0-9\s&.\-/'()]+$/),
      ],
    ],
    teamleaderid: [0, [Validators.required, Validators.min(1)]],
    description: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(500),
        Validators.pattern(/^[a-zA-Z0-9\s.,!?'"()&/\-]+$/),
      ],
    ],
    status: [RecordStatus.Active, Validators.required],
  });

  // Confirmation Modal
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
    const url = this.router.url.toLowerCase();
    if (url.includes('/deactive') || url.includes('/inactive')) {
      this.status = 2;
    } else {
      this.status = 1;
    }
    this.load();
    this.loadCounts();
    this.loadLookups();
    this.loadPendingRequests();
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
    const targetUrl = tabStatus === 2 ? '/marketing-team/deactive' : '/marketing-team';
    this.router.navigateByUrl(targetUrl);
    this.load();
  }

  get memberRoleOptions(): SelectOption[] {
    return this.roleOptions.filter((r) => r.value !== SalesmanRole.TeamLeader);
  }

  loadPendingRequests(): void {
    this.loadingPendingRequests = true;
    this.cdr.markForCheck();
    this.salesmenApi
      .getPendingDeactivationRequests()
      .pipe(
        finalize(() => {
          this.loadingPendingRequests = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (requests) => {
          this.pendingRequests = requests || [];
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.toast.error(this.readError(err), 'Failed to load deactivation requests');
        },
      });
  }

  openPendingModal(): void {
    this.isPendingModalOpen = true;
    this.loadPendingRequests();
    this.cdr.markForCheck();
  }

  closePendingModal(): void {
    this.isPendingModalOpen = false;
    this.cdr.markForCheck();
  }

  isLeader(req: SalesmanDeactivationRequest): boolean {
    if (req.isLeader !== undefined && req.isLeader !== null) return req.isLeader;
    const r = (req.roleInTeam || req.role || '').toLowerCase();
    return r.includes('lead');
  }

  getRoleDisplay(req: SalesmanDeactivationRequest): string {
    return this.isLeader(req) ? 'Team Leader' : 'Team Member';
  }

  approveRequest(req: SalesmanDeactivationRequest): void {
    this.processingRequestId = req.id;
    this.cdr.markForCheck();
    this.salesmenApi.approveDeactivation(req.id).subscribe({
      next: (res) => {
        this.processingRequestId = null;
        this.toast.success(res.message || 'Deactivation request approved successfully.');
        this.loadPendingRequests();
        this.load();
        this.loadLookups();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.processingRequestId = null;
        this.toast.error(this.readError(err), 'Approval Failed');
        this.cdr.markForCheck();
      },
    });
  }

  rejectRequest(req: SalesmanDeactivationRequest): void {
    this.processingRequestId = req.id;
    this.cdr.markForCheck();
    this.salesmenApi.rejectDeactivation(req.id).subscribe({
      next: (res) => {
        this.processingRequestId = null;
        this.toast.info(res.message || 'Deactivation request rejected.');
        this.loadPendingRequests();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.processingRequestId = null;
        this.toast.error(this.readError(err), 'Rejection Failed');
        this.cdr.markForCheck();
      },
    });
  }

  loadLookups(): void {
    forkJoin({
      salesmen: this.salesmenApi.getActiveLookup(),
      roles: this.api.getRoleOptions(),
    }).subscribe({
      next: ({ salesmen, roles }) => {
        this.activeSalesmen = salesmen;
        this.roleOptions = roles;
        this.cdr.markForCheck();
      },
    });
  }

  load(): void {
    this.loading = true;
    this.cdr.markForCheck();
    this.api
      .getList(this.search, this.status, this.pageNumber, this.pageSize)
      .pipe(
        timeout(8000),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (result) => {
          this.result = result;
          this.applySort();
          this.cdr.markForCheck();
        },
        error: (error) => {
          this.toast.error(this.readError(error), 'Failed to load teams');
          this.cdr.markForCheck();
        },
      });
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

  searchTeams(): void {
    this.pageNumber = 1;
    this.result.items = [];
    this.load();
  }

  onStatusChange(val: string): void {
    this.status = val === '' ? null : Number(val);
    this.pageNumber = 1;
    this.result.items = [];
    this.load();
  }

  toggleSort(col: 'teamcode' | 'teamname'): void {
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

  changePage(page: number): void {
    if (page < 1 || page > this.result.totalPages || page === this.pageNumber) return;
    this.pageNumber = page;
    this.load();
  }

  // --- View Modal ---
  openViewModal(teamItem: TeamListItem): void {
    this.viewingHeader = teamItem;
    this.isViewModalOpen = true;
    this.loadingView = true;
    this.viewingTeam = null;
    this.cdr.markForCheck();

    this.api.getById(teamItem.id).subscribe({
      next: (team) => {
        this.viewingTeam = team;
        this.loadingView = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loadingView = false;
        this.toast.error(this.readError(err), 'Cannot Load Team');
        this.closeViewModal();
      },
    });
  }

  closeViewModal(): void {
    this.isViewModalOpen = false;
    this.viewingTeam = null;
    this.viewingHeader = null;
    this.cdr.markForCheck();
  }

  get totalTargetInView(): number {
    return this.viewingTeam?.totalTargetAmount ||
      (this.viewingTeam?.members || []).reduce((acc, m) => acc + (Number(m.targetamount) || 0), 0);
  }

  // --- Create / Edit Modal ---
  openCreateModal(): void {
    this.isEditMode = false;
    this.editingId = null;
    this.formError = '';
    this.codeConflictError = '';
    this.members = [];
    this.form.reset({
      teamcode: '',
      teamname: '',
      teamleaderid: 0,
      description: '',
      status: RecordStatus.Active,
    });
    this.isEditModalOpen = true;
    this.cdr.markForCheck();
  }

  openEditModal(teamItem: TeamListItem | Team): void {
    this.isEditMode = true;
    this.editingId = teamItem.id;
    this.formError = '';
    this.codeConflictError = '';
    this.isEditModalOpen = true;
    this.cdr.markForCheck();

    this.api.getById(teamItem.id).subscribe({
      next: (team) => {
        this.form.patchValue({
          teamcode: team.teamcode,
          teamname: team.teamname,
          teamleaderid: team.teamleaderid,
          description: team.description || '',
          status: team.status,
        });

        this.members = team.members.map((m) => ({
          id: m.id,
          salesmanid: m.salesmanid,
          salesmancode: m.salesmancode || this.salesmanCode(m.salesmanid),
          salesmanname: m.salesmanname || this.salesmanName(m.salesmanid),
          role: m.role,
          targetamount: m.targetamount,
          status: m.status,
        }));
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.toast.error(this.readError(err), 'Error');
        this.closeEditModal();
      },
    });
  }

  closeEditModal(): void {
    this.isEditModalOpen = false;
    this.editingId = null;
    this.members = [];
    this.cdr.markForCheck();
  }

  onCodeInput(): void {
    const current = this.form.controls.teamcode.value;
    if (current) {
      this.form.controls.teamcode.setValue(current.toUpperCase(), { emitEvent: false });
    }
    this.codeConflictError = '';
  }

  get eligibleLeaders(): (SalesmanLookup & { displayText: string })[] {
    return this.activeSalesmen
      .filter((s) => {
        // Must be active in salesman table
        if (s.status !== 1) return false;
        // Must not belong to another active team (unless it is current team being edited)
        if (s.activeTeamId && s.activeTeamId !== this.editingId) return false;
        return true;
      })
      .map((s) => ({
        ...s,
        displayText: `${s.salesmanname} (${s.salesmancode})`,
      }));
  }

  onLeaderChanged(newLeaderId?: any): void {
    const rawVal = typeof newLeaderId === 'object' && newLeaderId !== null ? newLeaderId.value : newLeaderId;
    const leaderId = rawVal ? Number(rawVal) : Number(this.form.controls.teamleaderid.value);
    if (!leaderId) return;

    if (this.form.controls.teamleaderid.value !== leaderId) {
      this.form.controls.teamleaderid.setValue(leaderId);
    }

    const leaderSalesman = this.activeSalesmen.find((s) => s.id === leaderId);
    const leaderCode = leaderSalesman?.salesmancode || this.salesmanCode(leaderId);
    const leaderName = leaderSalesman?.salesmanname || this.salesmanName(leaderId);

    const leaderRowIndex = this.members.findIndex((m) => m.role === SalesmanRole.TeamLeader);

    if (leaderRowIndex >= 0) {
      const duplicateIndex = this.members.findIndex(
        (m, idx) => idx !== leaderRowIndex && m.salesmanid === leaderId
      );
      if (duplicateIndex >= 0) {
        this.members.splice(duplicateIndex, 1);
      }
      this.members[leaderRowIndex].salesmanid = leaderId;
      this.members[leaderRowIndex].salesmancode = leaderCode;
      this.members[leaderRowIndex].salesmanname = leaderName;
    } else {
      this.members = this.members.filter((m) => m.salesmanid !== leaderId);
      this.members.unshift({
        salesmanid: leaderId,
        salesmancode: leaderCode,
        salesmanname: leaderName,
        role: SalesmanRole.TeamLeader,
        targetamount: 0,
        status: RecordStatus.Active,
      });
    }
    this.formError = '';
    this.cdr.markForCheck();
  }

  addMember(): void {
    this.formError = '';
    this.members.push({
      salesmanid: 0,
      salesmancode: '',
      salesmanname: '',
      role: 0,
      targetamount: 0,
      status: RecordStatus.Active,
    });
    this.cdr.markForCheck();
  }

  removeMember(index: number): void {
    const member = this.members[index];
    if (member.role === SalesmanRole.TeamLeader) {
      this.formError = 'The Team Leader row cannot be removed. To change the leader, select a different Team Leader in the header.';
      this.toast.warning(this.formError);
      this.cdr.markForCheck();
      return;
    }

    this.members.splice(index, 1);
    this.formError = '';
    this.cdr.markForCheck();
  }

  onMemberSalesmanChange(member: TeamMember, newSalesmanId: any): void {
    const rawVal = typeof newSalesmanId === 'object' && newSalesmanId !== null ? newSalesmanId.value : newSalesmanId;
    const sId = Number(rawVal);
    if (!sId) return;

    member.salesmanid = sId;
    const s = this.activeSalesmen.find((x) => x.id === sId);
    member.salesmancode = s?.salesmancode || this.salesmanCode(sId);
    member.salesmanname = s?.salesmanname || this.salesmanName(sId);

    if (member.role === SalesmanRole.TeamLeader) {
      this.form.controls.teamleaderid.setValue(sId);
    }
    this.cdr.markForCheck();
  }

  onMemberRoleChange(member: TeamMember, newRole: number): void {
    if (newRole === SalesmanRole.TeamLeader) {
      const oldLeader = this.members.find((m) => m !== member && m.role === SalesmanRole.TeamLeader);
      if (oldLeader) {
        oldLeader.role = SalesmanRole.SalesExecutive;
      }
      this.form.controls.teamleaderid.setValue(member.salesmanid);
    }
    member.role = newRole;
    this.cdr.markForCheck();
  }

  availableSalesmen(current: TeamMember): (SalesmanLookup & { displayText: string })[] {
    return this.activeSalesmen
      .filter((s) => {
        // Always allow currently selected salesman in this row
        if (s.id === current.salesmanid) return true;
        // Must be active
        if (s.status !== 1) return false;
        // Must not be in another active team (unless current team being edited)
        if (s.activeTeamId && s.activeTeamId !== this.editingId) return false;
        // Must not already be assigned in another row in this team (prevent duplicates)
        if (this.members.some((m) => m !== current && m.salesmanid === s.id)) return false;
        return true;
      })
      .map((s) => ({
        ...s,
        displayText: `${s.salesmanname} (${s.salesmancode})`,
      }));
  }

  salesmanName(id: number): string {
    return this.activeSalesmen.find((s) => s.id === id)?.salesmanname || '';
  }

  salesmanCode(id: number): string {
    return this.activeSalesmen.find((s) => s.id === id)?.salesmancode || '';
  }

  get totalTarget(): number {
    return this.members.reduce((total, member) => total + (Number(member.targetamount) || 0), 0);
  }

  getErrorMessage(field: string): string {
    const control = this.form.get(field);
    if (!control || !control.errors || !control.touched) return '';

    if (control.hasError('required')) {
      switch (field) {
        case 'teamcode': return 'Team code is required.';
        case 'teamname': return 'Team name is required.';
        case 'teamleaderid': return 'Please select a Team Leader.';
        case 'description': return 'Description is required.';
        case 'status': return 'Status is required.';
        default: return 'This field is required.';
      }
    }
    if (control.hasError('min') && field === 'teamleaderid') {
      return 'Please select a Team Leader.';
    }
    if (control.hasError('minlength')) {
      return `Must be at least ${control.errors['minlength'].requiredLength} characters.`;
    }
    if (control.hasError('maxlength')) {
      return `Cannot exceed ${control.errors['maxlength'].requiredLength} characters.`;
    }
    if (control.hasError('pattern')) {
      switch (field) {
        case 'teamcode': return 'Only alphanumeric characters (A-Z, 0-9) allowed.';
        case 'teamname': return 'Only English characters, numbers, and standard symbols (&, -, ., /, \') are allowed.';
        case 'description': return 'Only English characters and standard punctuation are allowed.';
        default: return 'Invalid format.';
      }
    }
    return 'Invalid value.';
  }

  isFieldInvalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && control.touched;
  }

  saveTeam(): void {
    this.codeConflictError = '';
    this.formError = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please fix validation errors in the team header.');
      this.cdr.markForCheck();
      return;
    }

    const leaderId = Number(this.form.controls.teamleaderid.value);

    // Rule R4: Team must have at least one member row
    if (this.members.length === 0) {
      this.formError = 'Add at least one salesman to the team (Rule R4).';
      this.toast.error(this.formError, 'Rule R4');
      this.cdr.markForCheck();
      return;
    }

    // Check each member row has a valid salesman selected
    const unselectedMember = this.members.some((m) => !m.salesmanid || m.salesmanid <= 0);
    if (unselectedMember) {
      this.formError = 'All member rows must have a salesman selected.';
      this.toast.error(this.formError, 'Validation Error');
      this.cdr.markForCheck();
      return;
    }

    // Check each member row has a valid role selected
    const unselectedRole = this.members.some((m) => !m.role || m.role <= 0);
    if (unselectedRole) {
      this.formError = 'All member rows must have a position selected.';
      this.toast.error(this.formError, 'Validation Error');
      this.cdr.markForCheck();
      return;
    }

    // Rule R6: Exactly one row has role Team Leader, matching header Team Leader
    const leaderRows = this.members.filter((m) => m.role === SalesmanRole.TeamLeader);
    if (leaderRows.length !== 1 || leaderRows[0].salesmanid !== leaderId) {
      this.formError = 'The Team Leader in the header must be the only member with role Team Leader (Rule R6).';
      this.toast.error(this.formError, 'Rule R6');
      this.cdr.markForCheck();
      return;
    }

    // Rule R5: Same salesman cannot repeat in one team
    const salesmanIds = this.members.map((m) => m.salesmanid);
    const hasDuplicates = new Set(salesmanIds).size !== salesmanIds.length;
    if (hasDuplicates) {
      this.formError = 'The same salesman cannot appear twice in one team (Rule R5).';
      this.toast.error(this.formError, 'Rule R5');
      this.cdr.markForCheck();
      return;
    }

    // Rule R9: Target amount >= 0 and reasonable limit
    const hasNegativeTarget = this.members.some(
      (m) => Number(m.targetamount) < 0 || isNaN(Number(m.targetamount)) || Number(m.targetamount) > 999999999.99
    );
    if (hasNegativeTarget) {
      this.formError = 'Target amount must be between 0 and 999,999,999.99 (Rule R9).';
      this.toast.error(this.formError, 'Rule R9');
      this.cdr.markForCheck();
      return;
    }

    this.savingTeam = true;
    this.cdr.markForCheck();
    const value = this.form.getRawValue();

    const request = {
      teamcode: this.isEditMode ? undefined : value.teamcode.trim().toUpperCase(),
      teamname: value.teamname.trim(),
      teamleaderid: leaderId,
      description: value.description?.trim() || undefined,
      status: Number(value.status),
      members: this.members.map((m) => ({
        id: m.id || undefined,
        salesmanid: Number(m.salesmanid),
        role: Number(m.role),
        targetamount: Number(m.targetamount) || 0,
        status: Number(m.status),
      })),
    };

    const action = this.isEditMode
      ? this.api.update(this.editingId!, request)
      : this.api.create(request);

    action.subscribe({
      next: (team) => {
        this.savingTeam = false;
        this.toast.success(
          `Marketing Team ${team.teamcode} saved successfully.`
        );
        this.closeEditModal();
        this.load();
        this.loadCounts();
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.savingTeam = false;
        const msg = this.readError(error);
        if (error.status === 409 || msg.toLowerCase().includes('already exists')) {
          this.codeConflictError = msg || `Team code ${value.teamcode} already exists.`;
        } else {
          this.formError = msg;
          this.toast.error(msg, 'Save Team Failed');
        }
        this.cdr.markForCheck();
      },
    });
  }

  // Delete team confirmation matching Wireframe Page 8
  confirmDelete(team: TeamListItem): void {
    this.modalTitle = 'Delete Marketing Team';
    this.modalMessage = `Delete team ${team.teamcode} and its ${team.salesmancount} salesman assignments?`;
    this.modalSubMessage = 'Rule R11: Deleting a team cascades to remove its detail rows permanently.';
    this.modalConfirmText = 'Delete';
    this.modalIconType = 'danger';
    this.modalConfirmBtnClass = 'btn-danger';

    this.pendingAction = () => {
      this.modalProcessing = true;
      this.cdr.markForCheck();
      this.api.delete(team.id).subscribe({
        next: () => {
          this.modalProcessing = false;
          this.isModalOpen = false;
          this.toast.success(`Team ${team.teamcode} deleted successfully.`);
          this.cdr.markForCheck();
          this.load();
          this.loadCounts();
        },
        error: (error) => {
          this.modalProcessing = false;
          this.isModalOpen = false;
          this.toast.error(this.readError(error), 'Cannot Delete Team');
          this.cdr.markForCheck();
        },
      });
    };

    this.isModalOpen = true;
    this.cdr.markForCheck();
  }

  confirmStatusToggle(team: TeamListItem): void {
    const isActivating = team.status === 2;
    this.modalTitle = isActivating ? 'Activate Marketing Team' : 'Deactivate Marketing Team';
    this.modalMessage = isActivating
      ? `Activate team ${team.teamcode} (${team.teamname})?`
      : `Deactivate team ${team.teamcode} (${team.teamname})?`;
    this.modalSubMessage = isActivating
      ? 'Team will be set to Active.'
      : 'Deactivating this team will also deactivate all its member detail rows.';
    this.modalConfirmText = isActivating ? 'Activate' : 'Deactivate';
    this.modalIconType = isActivating ? 'info' : 'warning';
    this.modalConfirmBtnClass = isActivating ? 'btn-success' : 'btn-warning';

    this.pendingAction = () => {
      this.modalProcessing = true;
      this.cdr.markForCheck();
      const request = isActivating ? this.api.activate(team.id) : this.api.deactivate(team.id);
      request.subscribe({
        next: () => {
          this.modalProcessing = false;
          this.isModalOpen = false;
          this.toast.success(`Team ${team.teamcode} is now ${isActivating ? 'Active' : 'Inactive'}.`);
          this.cdr.markForCheck();
          this.load();
          this.loadCounts();
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
  }

  onModalConfirm(): void {
    if (this.pendingAction) this.pendingAction();
  }

  onModalCancel(): void {
    this.isModalOpen = false;
    this.pendingAction = null;
    this.cdr.markForCheck();
  }

  private readError(error: any): string {
    return error?.error?.message || error?.message || 'The request could not be completed.';
  }
}