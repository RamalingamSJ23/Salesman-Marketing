import { inject } from '@angular/core';
import { Routes, Router } from '@angular/router';
import { authGuard, salesmanGuard, marketingGuard } from './guards/auth.guard';
import { AuthService } from './services/auth.service';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [
      () => {
        const auth = inject(AuthService);
        const router = inject(Router);
        if (!auth.isAuthenticated()) {
          return router.parseUrl('/login');
        }
        return router.parseUrl(auth.getDefaultRoute());
      },
    ],
    children: [],
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'salesman',
    loadComponent: () => import('./pages/salesman-list/salesman-list.component').then((m) => m.SalesmanListComponent),
    canActivate: [salesmanGuard],
  },
  {
    path: 'salesman/deactive',
    loadComponent: () => import('./pages/salesman-list/salesman-list.component').then((m) => m.SalesmanListComponent),
    canActivate: [salesmanGuard],
  },
  {
    path: 'salesman/inactive',
    redirectTo: 'salesman/deactive',
  },
  {
    path: 'salesman/new',
    loadComponent: () => import('./pages/salesman-form/salesman-form.component').then((m) => m.SalesmanFormComponent),
    canActivate: [salesmanGuard],
  },
  {
    path: 'salesman/:id/edit',
    loadComponent: () => import('./pages/salesman-form/salesman-form.component').then((m) => m.SalesmanFormComponent),
    canActivate: [salesmanGuard],
  },
  {
    path: 'marketing-team',
    loadComponent: () => import('./pages/team-list/team-list.component').then((m) => m.TeamListComponent),
    canActivate: [marketingGuard],
  },
  {
    path: 'marketing-team/deactive',
    loadComponent: () => import('./pages/team-list/team-list.component').then((m) => m.TeamListComponent),
    canActivate: [marketingGuard],
  },
  {
    path: 'marketing-team/inactive',
    redirectTo: 'marketing-team/deactive',
  },
  {
    path: 'marketing-team/new',
    loadComponent: () => import('./pages/team-form/team-form.component').then((m) => m.TeamFormComponent),
    canActivate: [marketingGuard],
  },
  {
    path: 'marketing-team/:id/edit',
    loadComponent: () => import('./pages/team-form/team-form.component').then((m) => m.TeamFormComponent),
    canActivate: [marketingGuard],
  },
  {
    path: 'marketing-team/:id',
    loadComponent: () => import('./pages/team-view/team-view.component').then((m) => m.TeamViewComponent),
    canActivate: [marketingGuard],
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];
