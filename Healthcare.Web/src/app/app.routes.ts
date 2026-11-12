
import { Routes } from '@angular/router';

import { Login } from './features/auth/login/login';
import { authGuard } from './core/guards/auth-guard';
import { MainLayout } from './shared/layout/main-layout/main-layout';

export const routes: Routes = [
  {
    path: 'login',
    component: Login
  },
  {
    path: '',
    canActivate: [authGuard],
    component: MainLayout,
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard').then(
            component => component.Dashboard
          )
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
