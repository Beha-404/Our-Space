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
        path: 'forgot-password',
        canActivate: [guestGuard],
        loadComponent: () => import('./forgot-password-page/forgot-password-page').then(m => m.ForgotPasswordPage)
    },
    {
        path: '',
        canActivate: [authGuard],
        loadComponent: () => import('./shell/shell').then(m => m.Shell),
        children: [
            {
                path: 'home',
                loadComponent: () => import('./home-page/home-page').then(m => m.HomePage)
            },
            {
                path: 'profile',
                loadComponent: () => import('./profile-page/profile-page').then(m => m.ProfilePage)
            },
            {
                path: 'events',
                loadComponent: () => import('./events-page/events-page').then(m => m.EventsPage)
            },
            {
                path: 'memories',
                loadComponent: () => import('./memories-page/memories-page').then(m => m.MemoriesPage)
            },
            {
                path: 'wishlist',
                loadComponent: () => import('./wishlist-page/wishlist-page').then(m => m.WishlistPage)
            },
            {
                path: 'capsules',
                loadComponent: () => import('./capsules-page/capsules-page').then(m => m.CapsulesPage)
            },
        ]
    },
    {
        path: '**',
        redirectTo: ''
    },

];
