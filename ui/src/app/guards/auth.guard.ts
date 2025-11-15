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

        // Only store the intended route if it's not a public auth route
        const publicRoutes = ['/login', '/register', '/verify-email', '/auth/login', '/auth/register', '/auth/verify-email'];
        const isPublicRoute = publicRoutes.some(route => url.startsWith(route));
        
        if (!isPublicRoute) {
            // Store the attempted URL for redirecting after login
            this.authService.setIntendedRoute(url);
            console.log('AuthGuard - stored intended route:', url);
        } else {
            console.log('AuthGuard - skipping intended route storage for public route:', url);
        }
        
        console.log('AuthGuard - redirecting to login');

        // Redirect to login page
        this.router.navigate(['/login']);
        return false;
    }
}