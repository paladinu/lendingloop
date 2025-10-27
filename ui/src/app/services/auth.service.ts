import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap, catchError, throwError } from 'rxjs';
import {
    LoginRequest,
    RegisterRequest,
    AuthResponse,
    RegisterResponse,
    UserProfile,
    VerifyEmailRequest,
    VerificationResponse
} from '../models/auth.interface';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly API_URL = 'http://localhost:8080/api/auth';
    private readonly TOKEN_KEY = 'auth_token';
    private readonly USER_KEY = 'current_user';

    private currentUserSubject = new BehaviorSubject<UserProfile | null>(null);
    public currentUser$ = this.currentUserSubject.asObservable();

    constructor(private http: HttpClient) {
        this.loadStoredUser();
    }

    login(email: string, password: string): Observable<AuthResponse> {
        const loginRequest: LoginRequest = { email, password };


        return this.http.post<AuthResponse>(`${this.API_URL}/login`, loginRequest)
            .pipe(
                tap(response => {
                    this.storeAuthData(response);
                }),
                catchError(error => {
                    console.error('Login error:', error);
                    return throwError(() => error);
                })
            );
    }

    register(userData: RegisterRequest): Observable<RegisterResponse> {
        return this.http.post<RegisterResponse>(`${this.API_URL}/register`, userData)
            .pipe(
                catchError(error => {
                    console.error('Registration error:', error);
                    return throwError(() => error);
                })
            );
    }

    logout(): void {
        // Call logout endpoint
        this.http.post(`${this.API_URL}/logout`, {}).subscribe({
            next: () => {
                this.clearAuthData();
            },
            error: (error) => {
                console.error('Logout error:', error);
                // Clear local data even if server call fails
                this.clearAuthData();
            }
        });
    }

    getCurrentUser(): Observable<UserProfile | null> {
        return this.currentUser$;
    }

    isAuthenticated(): boolean {
        const token = this.getToken();
        console.log('AuthService - checking authentication, token exists:', !!token);

        if (!token) {
            return false;
        }

        // Check if token is expired
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const currentTime = Math.floor(Date.now() / 1000);
            // Add a small buffer (30 seconds) to account for clock skew
            const isValid = payload.exp > (currentTime - 30);

            // Convert timestamps to readable dates for debugging
            const expDate = new Date(payload.exp * 1000);
            const currentDate = new Date(currentTime * 1000);

            console.log('AuthService - token validation:', {
                exp: payload.exp,
                currentTime: currentTime,
                expDate: expDate.toISOString(),
                currentDate: currentDate.toISOString(),
                timeDifference: payload.exp - currentTime,
                isValid: isValid
            });
            return isValid;
        } catch (error) {
            console.error('Error parsing token:', error);
            return false;
        }
    }

    verifyEmail(token: string): Observable<VerificationResponse> {
        const verifyRequest: VerifyEmailRequest = { token };

        return this.http.post<VerificationResponse>(`${this.API_URL}/verify-email`, verifyRequest)
            .pipe(
                catchError(error => {
                    console.error('Email verification error:', error);
                    return throwError(() => error);
                })
            );
    }

    resendVerificationEmail(email: string): Observable<{ message: string }> {
        const resendRequest = { email };

        return this.http.post<{ message: string }>(`${this.API_URL}/resend-verification`, resendRequest)
            .pipe(
                catchError(error => {
                    console.error('Resend verification email error:', error);
                    return throwError(() => error);
                })
            );
    }

    getToken(): string | null {
        return localStorage.getItem(this.TOKEN_KEY);
    }

    private storeAuthData(authResponse: AuthResponse): void {
        localStorage.setItem(this.TOKEN_KEY, authResponse.token);
        localStorage.setItem(this.USER_KEY, JSON.stringify(authResponse.user));
        this.currentUserSubject.next(authResponse.user);
    }

    private clearAuthData(): void {
        localStorage.removeItem(this.TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);
        this.currentUserSubject.next(null);
    }

    private loadStoredUser(): void {
        const storedUser = localStorage.getItem(this.USER_KEY);
        if (storedUser && this.isAuthenticated()) {
            try {
                const user = JSON.parse(storedUser);
                this.currentUserSubject.next(user);
            } catch (error) {
                console.error('Error parsing stored user:', error);
                this.clearAuthData();
            }
        } else {
            this.clearAuthData();
        }
    }
}