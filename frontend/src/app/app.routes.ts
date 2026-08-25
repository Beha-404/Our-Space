import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { guestGuard } from './guards/guest.guard';

export const routes: Routes = [
    {
        path: '',
        pathMatch: 'full',
        canActivate: [guestGuard],
        loadComponent: () => import('./landing-page/landing-page').then(m => m.LandingPage)
    },
    {
        path: 'login',
        canActivate: [guestGuard],
        loadComponent: () => import('./login-page/login-page').then(m => m.LoginPage)
    },
    {
        path: 'register',
        canActivate: [guestGuard],
        loadComponent: () => import('./register-page/register-page').then(m => m.RegisterPage)
    },
    {
        path: 'home',
        canActivate: [authGuard],
        loadComponent: () => import('./home-page/home-page').then(m => m.HomePage)
    },
    {
        path: 'profile',
        canActivate: [authGuard],
        loadComponent: () => import('./profile-page/profile-page').then(m => m.ProfilePage)
    },
    {
        path: 'events',
        canActivate: [authGuard],
        loadComponent: () => import('./events-page/events-page').then(m => m.EventsPage)
    },
    {
        path: 'memories',
        canActivate: [authGuard],
        loadComponent: () => import('./memories-page/memories-page').then(m => m.MemoriesPage)
    },
    {
        path: '**',
        redirectTo: ''
    },

];
