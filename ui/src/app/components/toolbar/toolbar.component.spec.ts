import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToolbarComponent } from './toolbar.component';
import { AuthService } from '../../services/auth.service';
import { ItemRequestService } from '../../services/item-request.service';
import { NotificationService } from '../../services/notification.service';
import { Router, ActivatedRoute } from '@angular/router';
import { of, NEVER } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('ToolbarComponent', () => {
  let component: ToolbarComponent;
  let fixture: ComponentFixture<ToolbarComponent>;
  let mockAuthService: any;
  let mockItemRequestService: any;
  let mockNotificationService: any;
  let mockRouter: any;
  let mockActivatedRoute: any;
  let consoleErrorSpy: jest.SpyInstance;

  beforeEach(async () => {
    // Suppress console.error during tests
    consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

    mockAuthService = {
      getCurrentUser: jest.fn().mockReturnValue(of(null)),
      currentUser$: NEVER,
      logout: jest.fn(),
      refreshCurrentUser: jest.fn().mockReturnValue(of(null))
    };
    mockItemRequestService = {
      getPendingRequests: jest.fn().mockReturnValue(of([]))
    };
    mockNotificationService = {
      getUnreadCount: jest.fn().mockReturnValue(of(0)),
      getNotifications: jest.fn().mockReturnValue(of([])),
      markAsRead: jest.fn().mockReturnValue(of({}))
    };
    mockRouter = {
      navigate: jest.fn()
    };
    mockActivatedRoute = {
      snapshot: { params: {} }
    };

    await TestBed.configureTestingModule({
      imports: [ToolbarComponent],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: ItemRequestService, useValue: mockItemRequestService },
        { provide: NotificationService, useValue: mockNotificationService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    fixture = TestBed.createComponent(ToolbarComponent);
    component = fixture.componentInstance;
    // Don't call detectChanges to prevent ngOnInit from running
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
    jest.clearAllMocks();
    consoleErrorSpy.mockRestore();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
