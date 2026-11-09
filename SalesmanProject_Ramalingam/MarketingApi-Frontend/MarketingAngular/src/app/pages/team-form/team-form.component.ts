import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, timeout, finalize } from 'rxjs';
import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import { MarketingTeamApiService } from '../../services/marketing-team-api.service';
import { SalesmanApiService } from '../../services/salesman-api.service';
import { ToastService } from '../../services/toast.service';
import { RecordStatus, SalesmanLookup, SalesmanRole, SelectOption, Team, TeamMember } from '../../shared/models';

@Component({
  selector: 'app-team-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink, DropDownListModule],
  templateUrl: './team-form.component.html',
})
export class TeamFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly teams = inject(MarketingTeamApiService);
  private readonly salesmen = inject(SalesmanApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

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

  id: number | null = null;
  members: TeamMember[] = [];
  activeSalesmen: SalesmanLookup[] = [];
  roleOptions: SelectOption[] = [];
  codeConflictError = '';
  generalError = '';
  saving = false;
  loading = true;

  get editMode(): boolean {
    return this.id !== null;
  }

  get totalTarget(): number {
    return this.members.reduce((total, member) => total + (Number(member.targetamount) || 0), 0);
  }

  ngOnInit(): void {
    const paramId = this.route.snapshot.paramMap.get('id');
    this.id = paramId ? Number(paramId) : null;

    if (this.id) {
      forkJoin({
        salesmen: this.salesmen.getActiveLookup(),
        roles: this.teams.getRoleOptions(),
        team: this.teams.getById(this.id),
      })
        .pipe(
          timeout(8000),
          finalize(() => (this.loading = false))
        )
        .subscribe({
          next: ({ salesmen, roles, team }) => this.loadData(salesmen, roles, team),
          error: (error) => this.finishWithError(error),
        });
    } else {
      forkJoin({
        salesmen: this.salesmen.getActiveLookup(),
        roles: this.teams.getRoleOptions(),
      })
        .pipe(
          timeout(8000),
          finalize(() => (this.loading = false))
        )
        .subscribe({
          next: ({ salesmen, roles }) => this.loadData(salesmen, roles),
          error: (error) => this.finishWithError(error),
        });
    }
  }

  loadData(salesmen: SalesmanLookup[], roles: SelectOption[], team?: Team): void {
    this.activeSalesmen = salesmen;
    this.roleOptions = roles;

    if (team) {
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
    }

    this.loading = false;
    this.cdr.markForCheck();
  }

  get eligibleLeaders(): (SalesmanLookup & { displayText: string })[] {
    return this.activeSalesmen
      .filter((s) => {
        if (s.status !== 1) return false;
        if (s.activeTeamId && s.activeTeamId !== this.id) return false;
        return true;
      })
      .map((s) => ({
        ...s,
        displayText: `${s.salesmanname} (${s.salesmancode})`,
      }));
  }

  // Spec Page 8: On select of Team Leader, auto-adds or updates a grid row with role Team Leader
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
      // Remove any duplicate if this salesman was already in the grid in another row
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
      // No leader row exists yet: remove this salesman from regular rows if present
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
    this.generalError = '';
    this.cdr.markForCheck();
  }

  get memberRoleOptions(): SelectOption[] {
    return this.roleOptions.filter((r) => r.value !== SalesmanRole.TeamLeader);
  }

  // Add Member row
  addMember(): void {
    this.generalError = '';
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
      this.generalError = 'The Team Leader row cannot be removed. To change the leader, select a different Team Leader in the header.';
      this.toast.warning(this.generalError);
      this.cdr.markForCheck();
      return;
    }

    this.members.splice(index, 1);
    this.generalError = '';
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
        if (s.id === current.salesmanid) return true;
        if (s.status !== 1) return false;
        if (s.activeTeamId && s.activeTeamId !== this.id) return false;
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

  onCodeInput(): void {
    const current = this.form.controls.teamcode.value;
    if (current) {
      this.form.controls.teamcode.setValue(current.toUpperCase(), { emitEvent: false });
    }
    this.codeConflictError = '';
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
        case 'teamcode': return 'Only letters and numbers (A-Z, 0-9) are allowed without spaces.';
        case 'teamname': return 'Only English characters, numbers, and standard symbols (&, -, ., /, \') are allowed.';
        case 'description': return 'Only English characters and standard punctuation are allowed.';
        default: return 'Invalid format.';
      }
    }
    return 'Invalid value.';
  }

  isMemberRowInvalid(member: TeamMember): boolean {
    return !member.salesmanid || member.salesmanid <= 0 || member.targetamount < 0 || isNaN(Number(member.targetamount));
  }

  save(): void {
    this.codeConflictError = '';
    this.generalError = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please fix validation errors in the team header.');
      this.cdr.markForCheck();
      return;
    }

    const leaderId = Number(this.form.controls.teamleaderid.value);

    // Rule R4: Team must have at least one member row
    if (this.members.length === 0) {
      this.generalError = 'Add at least one salesman to the team (Rule R4).';
      this.toast.error(this.generalError, 'Rule R4');
      this.cdr.markForCheck();
      return;
    }

    // Check each member row has a valid salesman selected
    const unselectedMember = this.members.some((m) => !m.salesmanid || m.salesmanid <= 0);
    if (unselectedMember) {
      this.generalError = 'All member rows must have a salesman selected.';
      this.toast.error(this.generalError, 'Validation Error');
      this.cdr.markForCheck();
      return;
    }

    // Check each member row has a valid position selected
    const unselectedRole = this.members.some((m) => !m.role || m.role <= 0);
    if (unselectedRole) {
      this.generalError = 'All member rows must have a position selected.';
      this.toast.error(this.generalError, 'Validation Error');
      this.cdr.markForCheck();
      return;
    }

    // Rule R6: Exactly one row has role Team Leader, matching header Team Leader
    const leaderRows = this.members.filter((m) => m.role === SalesmanRole.TeamLeader);
    if (leaderRows.length !== 1 || leaderRows[0].salesmanid !== leaderId) {
      this.generalError = 'The Team Leader in the header must be the only member with role Team Leader (Rule R6).';
      this.toast.error(this.generalError, 'Rule R6');
      this.cdr.markForCheck();
      return;
    }

    // Rule R5: Same salesman cannot repeat in one team
    const salesmanIds = this.members.map((m) => m.salesmanid);
    const hasDuplicates = new Set(salesmanIds).size !== salesmanIds.length;
    if (hasDuplicates) {
      this.generalError = 'The same salesman cannot appear twice in one team (Rule R5).';
      this.toast.error(this.generalError, 'Rule R5');
      this.cdr.markForCheck();
      return;
    }

    // Rule R9: Target amount >= 0 and reasonable limit
    const hasNegativeTarget = this.members.some(
      (m) => Number(m.targetamount) < 0 || isNaN(Number(m.targetamount)) || Number(m.targetamount) > 999999999.99
    );
    if (hasNegativeTarget) {
      this.generalError = 'Target amount must be between 0 and 999,999,999.99 (Rule R9).';
      this.toast.error(this.generalError, 'Rule R9');
      this.cdr.markForCheck();
      return;
    }

    this.saving = true;
    const value = this.form.getRawValue();

    const request = {
      teamcode: this.editMode ? undefined : value.teamcode.trim().toUpperCase(),
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

    const action = this.editMode
      ? this.teams.update(this.id!, request)
      : this.teams.create(request);

    action.subscribe({
      next: (team) => {
        this.saving = false;
        this.cdr.markForCheck();
        this.toast.success(
          `Marketing Team ${team.teamcode} saved successfully.`
        );
        // Spec Page 2: Save on a form returns to its list
        this.router.navigate(['/marketing-team']);
      },
      error: (error) => {
        this.saving = false;
        const msg = this.readError(error);
        if (error.status === 409 || msg.toLowerCase().includes('already exists')) {
          this.codeConflictError = msg || `Team code ${value.teamcode} already exists.`;
        } else {
          this.generalError = msg;
          this.toast.error(msg, 'Save Team Failed');
        }
        this.cdr.markForCheck();
      },
    });
  }

  isInvalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && control.touched;
  }

  private finishWithError(error: any): void {
    this.loading = false;
    this.generalError = this.readError(error);
    this.toast.error(this.generalError, 'Error');
    this.cdr.markForCheck();
  }

  private readError(error: any): string {
    return error?.error?.message || error?.message || 'The request could not be completed.';
  }
}
