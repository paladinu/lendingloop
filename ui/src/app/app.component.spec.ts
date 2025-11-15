import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { provideRouter } from '@angular/router';
import { AuthService } from './services/auth.service';
import { of } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('AppComponent', () => {
  let mockAuthService: jest.Mocked<Partial<AuthService>>;
  let consoleErrorSpy: jest.SpyInstance;

  beforeEach(async () => {
    // Suppress console.error during tests
    consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

    mockAuthService = {
      currentUser$: of(null),
      isAuthenticated: jest.fn().mockReturnValue(false)
    };

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();
  });

  afterEach(() => {
    consoleErrorSpy.mockRestore();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should have the correct title', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.title).toEqual('Shared Items Manager');
  });

  it('should initialize with no authenticated user', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.isAuthenticated).toBe(false);
    expect(app.currentUser).toBeNull();
  });
});
