import { Injectable } from '@angular/core';
import { CanActivate, CanActivateChild, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
    providedIn: 'root'
})
export class AuthGuard implements CanActivate, CanActivateChild {

    constructor(
        private authService: AuthService,
        private router: Router
    ) { }

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): boolean {
        return this.checkAuthentication(state.url);
    }

    canActivateChild(
        childRoute: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): boolean {
        return this.checkAuthentication(state.url);
    }

    private checkAuthentication(url: string): boolean {
        const isAuthenticated = this.authService.isAuthenticated();
        console.log('AuthGuard - checking authentication for URL:', url);
        console.log('AuthGuard - isAuthenticated:', isAuthenticated);

        if (isAuthenticated) {
            return true;
        }

        // Store the attempted URL for redirecting after login
        this.authService.setIntendedRoute(url);
        console.log('AuthGuard - redirecting to login');

        // Redirect to login page
        this.router.navigate(['/login']);
        return false;
    }
}