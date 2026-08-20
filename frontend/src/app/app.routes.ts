import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: 'login',
        loadComponent: () => import('./login-page/login-page').then(m => m.LoginPage)
    },
    {
        path: 'home',
        loadComponent: () => import('./home-page/home-page').then(m => m.HomePage)
    },
    {
        path: 'navbar',
        loadComponent: () => import('./navbar/navbar').then(m => m.Navbar)
    },
    {
        path: '**',
        redirectTo: 'home'
    },

];
