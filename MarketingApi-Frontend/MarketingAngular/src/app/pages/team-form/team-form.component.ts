import { CommonModule } from '@angular/common';
import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin, timeout } from 'rxjs';
import { ChangeEventArgs, DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import { MarketingTeamApiService } from '../../services/marketing-team-api.service';
import { SalesmanApiService } from '../../services/salesman-api.service';
import { ToastService } from '../../services/toast.service';
import {
  RecordStatus,
  SalesmanLookup,
  SalesmanRole,
  SelectOption,
  Team,
  TeamMember,
} from '../../models';
import { parseApiError } from '../../shared/api-error';
import { VALIDATION_PATTERNS, getControlErrorMessage } from '../../shared/validation';

const REQUIRED_LABELS: Record<string, string> = {
  teamcode: 'Team code is required.',
  teamname: 'Team name is required.',
  teamleaderid: 'Please select a Team Leader.',
  description: 'Description is required.',
  status: 'Status is required.',
};

const PATTERN_MESSAGES: Record<string, string> = {
  teamcode: 'Only alphanumeric characters (A-Z, 0-9) are allowed.',
  teamname: 'Team name contains unsupported characters.',
  description: 'Description contains unsupported characters.',
};

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
  private readonly destroyRef = inject(DestroyRef);

  readonly modalMode = input(false);
  readonly teamId = input<number | null>(null);
  readonly saved = output<Team>();
  readonly cancelled = output<void>();

  readonly form = this.fb.nonNullable.group({
    teamcode: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(20),
        Validators.pattern(VALIDATION_PATTERNS.code),
      ],
    ],
    teamname: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
        Validators.pattern(VALIDATION_PATTERNS.teamText),
      ],
    ],
    teamleaderid: [0, [Validators.required, Validators.min(1)]],
    description: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(500),
        Validators.pattern(VALIDATION_PATTERNS.teamText),
      ],
    ],
    status: [RecordStatus.Active as number, Validators.required],
  });

  readonly resolvedId = signal<number | null>(null);
  readonly editMode = computed(() => this.resolvedId() !== null);
  readonly members = signal<TeamMember[]>([]);
  readonly activeSalesmen = signal<SalesmanLookup[]>([]);
  readonly roleOptions = signal<SelectOption[]>([]);
  readonly codeConflictError = signal('');
  readonly generalError = signal('');
  readonly saving = signal(false);
  readonly loading = signal(true);

  readonly totalTarget = computed(() =>
    this.members().reduce((total, member) => total + (Number(member.targetamount) || 0), 0)
  );

  readonly memberRoleOptions = computed(() =>
    this.roleOptions().filter((r) => r.value !== SalesmanRole.TeamLeader)
  );

  readonly eligibleLeaders = computed(() => {
    const currentTeamId = this.resolvedId();
    return this.activeSalesmen()
      .filter((s) => s.status === RecordStatus.Active && (!s.activeTeamId || s.activeTeamId === currentTeamId))
      .map((s) => ({
        ...s,
        displayText: `${s.salesmanname} (${s.salesmancode})`,
      }));
  });

  constructor() {
    effect(() => {
      if (!this.modalMode()) return;
      const id = this.teamId();
      this.resolvedId.set(id);
      this.resetForm();
      this.fetchLookupsAndTeam(id);
    });
  }

  ngOnInit(): void {
    if (this.modalMode()) {
      return;
    }

    const paramId = this.route.snapshot.paramMap.get('id');
    const id = paramId ? Number(paramId) : null;
    this.resolvedId.set(id);
    this.fetchLookupsAndTeam(id);
  }

  onLeaderChanged(event?: ChangeEventArgs): void {
    const rawVal = event?.value;
    const leaderId = rawVal ? Number(rawVal) : Number(this.form.controls.teamleaderid.value);
    if (!leaderId) return;

    if (this.form.controls.teamleaderid.value !== leaderId) {
      this.form.controls.teamleaderid.setValue(leaderId);
    }

    const leaderSalesman = this.activeSalesmen().find((s) => s.id === leaderId);
    const leaderCode = leaderSalesman?.salesmancode || this.salesmanCode(leaderId);
    const leaderName = leaderSalesman?.salesmanname || this.salesmanName(leaderId);

    this.members.update((rows) => {
      const next = rows.map((r) => ({ ...r }));
      const leaderIdx = next.findIndex((m) => m.role === SalesmanRole.TeamLeader);

      if (leaderIdx >= 0) {
        const filtered = next.filter((m, idx) => idx === leaderIdx || m.salesmanid !== leaderId);
        const targetIdx = filtered.findIndex((m) => m.role === SalesmanRole.TeamLeader);
        filtered[targetIdx] = {
          ...filtered[targetIdx],
          id: undefined, // changing leader on existing row is handled as remove + add
          salesmanid: leaderId,
          salesmancode: leaderCode,
          salesmanname: leaderName,
        };
        return filtered;
      }

      return [
        {
          salesmanid: leaderId,
          salesmancode: leaderCode,
          salesmanname: leaderName,
          role: SalesmanRole.TeamLeader,
          targetamount: 0,
          status: RecordStatus.Active,
        },
        ...next.filter((m) => m.salesmanid !== leaderId),
      ];
    });

    this.generalError.set('');
  }

  addMember(): void {
    this.generalError.set('');
    this.members.update((rows) => [
      ...rows,
      {
        salesmanid: 0,
        salesmancode: '',
        salesmanname: '',
        role: 0,
        targetamount: 0,
        status: RecordStatus.Active,
      },
    ]);
  }

  removeMember(index: number): void {
    const member = this.members()[index];
    if (member?.role === SalesmanRole.TeamLeader) {
      const msg = 'The Team Leader row cannot be removed. Select a different Team Leader in the header instead.';
      this.generalError.set(msg);
      this.toast.warning(msg);
      return;
    }

    this.members.update((rows) => rows.filter((_, idx) => idx !== index));
    this.generalError.set('');
  }

  onMemberSalesmanChange(index: number, event: ChangeEventArgs): void {
    const sId = Number(event?.value);
    if (!sId) return;

    const s = this.activeSalesmen().find((x) => x.id === sId);
    this.members.update((rows) =>
      rows.map((row, idx) => {
        if (idx !== index) return row;
        const salesmanChanged = row.salesmanid !== sId;
        return {
          ...row,
          id: salesmanChanged ? undefined : row.id,
          salesmanid: sId,
          salesmancode: s?.salesmancode || this.salesmanCode(sId),
          salesmanname: s?.salesmanname || this.salesmanName(sId),
        };
      })
    );

    const updated = this.members()[index];
    if (updated?.role === SalesmanRole.TeamLeader) {
      this.form.controls.teamleaderid.setValue(sId);
    }
  }

  onMemberRoleChange(index: number, newRole: number): void {
    this.members.update((rows) =>
      rows.map((row, idx) => {
        if (idx === index) {
          return { ...row, role: newRole };
        }
        if (newRole === SalesmanRole.TeamLeader && row.role === SalesmanRole.TeamLeader) {
          return { ...row, role: SalesmanRole.SalesExecutive };
        }
        return row;
      })
    );

    const updated = this.members()[index];
    if (newRole === SalesmanRole.TeamLeader && updated) {
      this.form.controls.teamleaderid.setValue(updated.salesmanid);
    }
  }

  availableSalesmen(current: TeamMember): (SalesmanLookup & { displayText: string })[] {
    const currentTeamId = this.resolvedId();
    const currentMembers = this.members();
    return this.activeSalesmen()
      .filter((s) => {
        if (s.id === current.salesmanid) return true;
        if (s.status !== RecordStatus.Active) return false;
        if (s.activeTeamId && s.activeTeamId !== currentTeamId) return false;
        if (currentMembers.some((m) => m !== current && m.salesmanid === s.id)) return false;
        return true;
      })
      .map((s) => ({
        ...s,
        displayText: `${s.salesmanname} (${s.salesmancode})`,
      }));
  }

  onCodeInput(): void {
    const current = this.form.controls.teamcode.value;
    if (current) {
      this.form.controls.teamcode.setValue(current.toUpperCase(), { emitEvent: false });
    }
    this.codeConflictError.set('');
  }

  getErrorMessage(field: string): string {
    return getControlErrorMessage(this.form, field, REQUIRED_LABELS, PATTERN_MESSAGES);
  }

  isInvalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && control.touched;
  }

  save(): void {
    this.codeConflictError.set('');
    this.generalError.set('');

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please fix validation errors in the team header.');
      return;
    }

    const leaderId = Number(this.form.controls.teamleaderid.value);
    const currentMembers = this.members();

    if (currentMembers.length === 0) {
      this.setValidationError('Add at least one salesman to the team (Rule R4).', 'Rule R4');
      return;
    }

    if (currentMembers.some((m) => !m.salesmanid || m.salesmanid <= 0)) {
      this.setValidationError('All member rows must have a salesman selected.');
      return;
    }

    if (currentMembers.some((m) => !m.role || m.role <= 0)) {
      this.setValidationError('All member rows must have a position selected.');
      return;
    }

    const leaderRows = currentMembers.filter((m) => m.role === SalesmanRole.TeamLeader);
    if (leaderRows.length !== 1 || leaderRows[0].salesmanid !== leaderId) {
      this.setValidationError('The Team Leader in the header must be the only member with role Team Leader (Rule R6).', 'Rule R6');
      return;
    }

    const salesmanIds = currentMembers.map((m) => m.salesmanid);
    if (new Set(salesmanIds).size !== salesmanIds.length) {
      this.setValidationError('The same salesman cannot appear twice in one team (Rule R5).', 'Rule R5');
      return;
    }

    if (currentMembers.some((m) => Number(m.targetamount) < 0 || Number.isNaN(Number(m.targetamount)) || Number(m.targetamount) > 999999999.99)) {
      this.setValidationError('Target amount must be between 0 and 999,999,999.99 (Rule R9).', 'Rule R9');
      return;
    }

    this.saving.set(true);
    const value = this.form.getRawValue();

    const request = {
      teamcode: this.editMode() ? undefined : value.teamcode.trim().toUpperCase(),
      teamname: value.teamname.trim(),
      teamleaderid: leaderId,
      description: value.description?.trim() || undefined,
      status: Number(value.status),
      members: currentMembers.map((m) => ({
        id: m.id || undefined,
        salesmanid: Number(m.salesmanid),
        role: Number(m.role),
        targetamount: Number(m.targetamount) || 0,
        status: Number(m.status),
      })),
    };

    const action = this.editMode()
      ? this.teams.update(this.resolvedId()!, request)
      : this.teams.create(request);

    action
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (team) => {
          this.toast.success(`Marketing Team ${team.teamcode} saved successfully.`);
          if (this.modalMode()) {
            this.saved.emit(team);
          } else {
            this.router.navigate(['/marketing-team']);
          }
        },
        error: (error) => {
          const parsed = parseApiError(error);
          if (parsed.field === 'teamcode') {
            this.codeConflictError.set(parsed.message);
          } else {
            this.generalError.set(parsed.message);
            this.toast.error(parsed.message, 'Save Team Failed');
          }
        },
      });
  }

  cancel(): void {
    if (this.modalMode()) {
      this.cancelled.emit();
    } else {
      this.router.navigate(['/marketing-team']);
    }
  }

  private fetchLookupsAndTeam(id: number | null): void {
    this.loading.set(true);

    if (id) {
      forkJoin({
        salesmen: this.salesmen.getActiveLookup(),
        roles: this.teams.getRoleOptions(),
        team: this.teams.getById(id),
      })
        .pipe(
          timeout(8000),
          finalize(() => this.loading.set(false)),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe({
          next: ({ salesmen, roles, team }) => this.applyLoadedData(salesmen, roles, team),
          error: (error) => this.handleLoadError(error),
        });
    } else {
      forkJoin({
        salesmen: this.salesmen.getActiveLookup(),
        roles: this.teams.getRoleOptions(),
      })
        .pipe(
          timeout(8000),
          finalize(() => this.loading.set(false)),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe({
          next: ({ salesmen, roles }) => this.applyLoadedData(salesmen, roles),
          error: (error) => this.handleLoadError(error),
        });
    }
  }

  private applyLoadedData(salesmen: SalesmanLookup[], roles: SelectOption[], team?: Team): void {
    this.activeSalesmen.set(salesmen);
    this.roleOptions.set(roles);

    if (team) {
      this.form.patchValue({
        teamcode: team.teamcode,
        teamname: team.teamname,
        teamleaderid: team.teamleaderid,
        description: team.description || '',
        status: team.status,
      });

      this.members.set(
        team.members.map((m) => ({
          id: m.id,
          salesmanid: m.salesmanid,
          salesmancode: m.salesmancode || this.salesmanCode(m.salesmanid),
          salesmanname: m.salesmanname || this.salesmanName(m.salesmanid),
          role: m.role,
          targetamount: m.targetamount,
          status: m.status,
        }))
      );
    }
  }

  private handleLoadError(error: unknown): void {
    const parsed = parseApiError(error, 'Failed to load team details.');
    this.generalError.set(parsed.message);
    this.toast.error(parsed.message, 'Error');
  }

  private setValidationError(message: string, title = 'Validation Error'): void {
    this.generalError.set(message);
    this.toast.error(message, title);
  }

  private resetForm(): void {
    this.codeConflictError.set('');
    this.generalError.set('');
    this.members.set([]);
    this.form.reset({
      teamcode: '',
      teamname: '',
      teamleaderid: 0,
      description: '',
      status: RecordStatus.Active,
    });
  }

  private salesmanName(id: number): string {
    return this.activeSalesmen().find((s) => s.id === id)?.salesmanname || '';
  }

  private salesmanCode(id: number): string {
    return this.activeSalesmen().find((s) => s.id === id)?.salesmancode || '';
  }
}
