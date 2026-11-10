import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { GridModule } from '@syncfusion/ej2-angular-grids';
import { MarketingTeamApiService } from '../../services/marketing-team-api.service';
import { ToastService } from '../../services/toast.service';
import { Team } from '../../models';
import { readApiError } from '../../shared/api-error';

@Component({
  selector: 'app-team-view',
  standalone: true,
  imports: [CommonModule, RouterLink, GridModule],
  templateUrl: './team-view.component.html',
  styleUrl: './team-view.component.css',
})
export class TeamViewComponent implements OnInit {
  private readonly api = inject(MarketingTeamApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly team = signal<Team | null>(null);
  readonly errorMessage = signal('');
  readonly loading = signal(true);

  readonly totalTarget = computed(() => {
    const t = this.team();
    if (!t) return 0;
    return t.totalTargetAmount ?? (t.members || []).reduce((acc, m) => acc + (Number(m.targetamount) || 0), 0);
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMessage.set('Invalid team identifier.');
      this.loading.set(false);
      return;
    }

    this.api
      .getById(id)
      .pipe(
        timeout(8000),
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (team) => this.team.set(team),
        error: (error) => {
          const msg = readApiError(error, 'The team could not be loaded.');
          this.errorMessage.set(msg);
          this.toast.error(msg, 'Failed to load team');
        },
      });
  }
}
