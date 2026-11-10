import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { timeout, finalize } from 'rxjs';
import { GridModule } from '@syncfusion/ej2-angular-grids';
import { MarketingTeamApiService } from '../../services/marketing-team-api.service';
import { ToastService } from '../../services/toast.service';
import { Team } from '../../shared/models';

@Component({
  selector: 'app-team-view',
  standalone: true,
  imports: [CommonModule, RouterLink, GridModule],
  templateUrl: './team-view.component.html',
})
export class TeamViewComponent implements OnInit {
  private readonly api = inject(MarketingTeamApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  team: Team | null = null;
  errorMessage = '';
  loading = true;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMessage = 'Invalid team identifier.';
      this.loading = false;
      this.cdr.markForCheck();
      return;
    }

    this.api
      .getById(id)
      .pipe(
        timeout(8000),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (team) => {
          this.team = team;
          this.cdr.markForCheck();
        },
        error: (error) => {
          this.errorMessage = this.readError(error);
          this.toast.error(this.errorMessage, 'Failed to load team');
          this.cdr.markForCheck();
        },
      });
  }

  get totalTarget(): number {
    return this.team?.totalTargetAmount || 
      (this.team?.members || []).reduce((acc, m) => acc + (Number(m.targetamount) || 0), 0);
  }

  private readError(error: any): string {
    return error?.error?.message || error?.message || 'The team could not be loaded.';
  }
}
