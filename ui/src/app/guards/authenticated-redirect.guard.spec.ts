import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { authenticatedRedirectGuard } from './authenticated-redirect.guard';
import { AuthService } from '../services/auth.service';

describe('authenticatedRedirectGuard', () => {
    let mockAuthService: { isAuthenticated: jest.Mock };
    let mockRouter: { navigate: jest.Mock };

    const executeGuard: CanActivateFn = (...guardParameters) =>
        TestBed.runInInjectionContext(() => authenticatedRedirectGuard(...guardParameters));

    beforeEach(() => {
        //arrange - set up mocks
        mockAuthService = { isAuthenticated: jest.fn() };
        mockRouter = { navigate: jest.fn() };

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: mockAuthService },
                { provide: Router, useValue: mockRouter }
            ]
        });
    });

    it('should return true for unauthenticated users without redirecting', () => {
        //arrange
        mockAuthService.isAuthenticated.mockReturnValue(false);

        //act
        const result = executeGuard({} as any, {} as any);

        //assert
        expect(result).toBe(true);
        expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('should navigate to /loops and return false for authenticated users', () => {
        //arrange
        mockAuthService.isAuthenticated.mockReturnValue(true);

        //act
        const result = executeGuard({} as any, {} as any);

        //assert
        expect(result).toBe(false);
        expect(mockRouter.navigate).toHaveBeenCalledWith(['/loops']);
    });

    it('should return true when isAuthenticated() throws an error (fail open)', () => {
        //arrange
        mockAuthService.isAuthenticated.mockImplementation(() => {
            throw new Error('Unexpected auth error');
        });

        //act
        const result = executeGuard({} as any, {} as any);

        //assert
        expect(result).toBe(true);
        expect(mockRouter.navigate).not.toHaveBeenCalled();
    });
});
