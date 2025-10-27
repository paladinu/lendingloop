import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { AuthLayoutComponent } from './components/auth-layout/auth-layout.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { EmailVerificationComponent } from './components/email-verification/email-verification.component';
import { MainComponent } from './components/main/main.component';

export const routes: Routes = [
    // Authentication routes (no guard needed)
    {
        path: 'auth',
        component: AuthLayoutComponent,
        children: [
            { path: 'login', component: LoginComponent },
            { path: 'register', component: RegisterComponent },
            { path: 'verify-email', component: EmailVerificationComponent },
            { path: '', redirectTo: 'login', pathMatch: 'full' }
        ]
    },

    // Convenience routes for direct access
    { path: 'login', redirectTo: 'auth/login', pathMatch: 'full' },
    { path: 'register', redirectTo: 'auth/register', pathMatch: 'full' },
    { path: 'verify-email', redirectTo: 'auth/verify-email', pathMatch: 'full' },

    // Protected main app route
    {
        path: '',
        component: MainComponent,
        canActivate: [AuthGuard]
    },

    // Fallback route
    { path: '**', redirectTo: 'auth/login' }
];
