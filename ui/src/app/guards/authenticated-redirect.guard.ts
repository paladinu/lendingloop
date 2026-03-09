import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authenticatedRedirectGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    try {
        if (authService.isAuthenticated()) {
            router.navigate(['/loops']);
            return false;
        }
        return true;
    } catch {
        return true;
    }
};
