import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { SalesmanApiService } from '../../services/salesman-api.service';
import { MarketingTeamApiService } from '../../services/marketing-team-api.service';
import { AuthService } from '../../services/auth.service';
import { Salesman, TeamListItem } from '../../shared/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private readonly salesmanApi = inject(SalesmanApiService);
  private readonly teamApi = inject(MarketingTeamApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly auth = inject(AuthService);

  loading = true;
  totalSalesmen = 0;
  activeSalesmen = 0;
  inactiveSalesmen = 0;

  totalTeams = 0;
  activeTeams = 0;
  inactiveTeams = 0;
  totalSalesmenAssigned = 0;

  recentSalesmen: Salesman[] = [];
  recentTeams: TeamListItem[] = [];

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.loading = true;
    this.cdr.markForCheck();

    forkJoin({
      salesmen: this.salesmanApi.getList('', null, 1, 100),
      teams: this.teamApi.getList('', null, 1, 100),
    }).subscribe({
      next: ({ salesmen, teams }) => {
        this.totalSalesmen = salesmen.totalCount;
        this.activeSalesmen = salesmen.items.filter((s) => s.status === 1).length;
        this.inactiveSalesmen = salesmen.items.filter((s) => s.status === 2).length;
        this.recentSalesmen = salesmen.items.slice(0, 5);

        this.totalTeams = teams.totalCount;
        this.activeTeams = teams.items.filter((t) => t.status === 1).length;
        this.inactiveTeams = teams.items.filter((t) => t.status === 2).length;
        this.totalSalesmenAssigned = teams.items.reduce((sum, t) => sum + (t.salesmancount || 0), 0);
        this.recentTeams = teams.items.slice(0, 5);

        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  get activeRate(): number {
    if (this.totalSalesmen === 0) return 0;
    return Math.round((this.activeSalesmen / this.totalSalesmen) * 100);
  }
}
