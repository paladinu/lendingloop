import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProfileComponent } from './profile.component';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { LoopScoreService } from '../../services/loop-score.service';
import { provideHttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { of, BehaviorSubject, throwError } from 'rxjs';
import { UserProfile, PublicProfile } from '../../models/auth.interface';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('ProfileComponent', () => {
  let component: ProfileComponent;
  let fixture: ComponentFixture<ProfileComponent>;
  let mockAuthService: any;
  let mockUserService: any;
  let mockActivatedRoute: any;
  let mockLocation: any;
  let paramMapSubject: BehaviorSubject<any>;

  const mockUser: UserProfile = {
    id: 'user123',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    streetAddress: '123 Test St',
    isEmailVerified: true,
    loopScore: 10,
    badges: []
  };

  const mockPublicProfile: PublicProfile = {
    userId: 'otherUser456',
    firstName: 'Other',
    lastName: 'User',
    loopScore: 25,
    badges: [],
    scoreHistory: []
  };

  beforeEach(async () => {
    // Create a BehaviorSubject for paramMap to simulate route parameter changes
    paramMapSubject = new BehaviorSubject({
      get: jest.fn().mockReturnValue(null)
    });

    mockAuthService = {
      getCurrentUser: jest.fn().mockReturnValue(of(mockUser)),
      refreshCurrentUser: jest.fn().mockReturnValue(of(mockUser))
    };

    mockUserService = {
      getPublicProfile: jest.fn().mockReturnValue(of(mockPublicProfile))
    };

    const mockNotificationService = {
      getUnreadCount: jest.fn().mockReturnValue(of(0))
    };

    const mockItemRequestService = {
      getPendingRequests: jest.fn().mockReturnValue(of([]))
    };

    const mockLoopScoreService = {
      getScoreHistory: jest.fn().mockReturnValue(of([])),
      getUserScoreAsync: jest.fn().mockReturnValue(of(10)),
      getScoreExplanation: jest.fn().mockReturnValue([]),
      getBadgeProgress: jest.fn().mockReturnValue(of(new Map())),
      getBadgeRarities: jest.fn().mockReturnValue(of(new Map())),
      getRarityColor: jest.fn().mockReturnValue('#9E9E9E'),
      getAllBadgeMetadata: jest.fn().mockReturnValue([
        { badgeType: 'Bronze', name: 'Bronze Badge', description: 'Awarded for reaching 10 points', category: 'milestone', requirement: 'Reach 10 points', icon: '🏆' },
        { badgeType: 'Silver', name: 'Silver Badge', description: 'Awarded for reaching 50 points', category: 'milestone', requirement: 'Reach 50 points', icon: '🏆' },
        { badgeType: 'Gold', name: 'Gold Badge', description: 'Awarded for reaching 100 points', category: 'milestone', requirement: 'Reach 100 points', icon: '🏆' },
        { badgeType: 'FirstLend', name: 'First Lend', description: 'Complete your first lending transaction', category: 'achievement', requirement: 'Lend an item for the first time', icon: '🎁' },
        { badgeType: 'ReliableBorrower', name: 'Reliable Borrower', description: 'Return items on time consistently', category: 'achievement', requirement: 'Complete 10 on-time returns', icon: '⭐' },
        { badgeType: 'GenerousLender', name: 'Generous Lender', description: 'Share your items frequently', category: 'achievement', requirement: 'Complete 50 lending transactions', icon: '🤝' },
        { badgeType: 'PerfectRecord', name: 'Perfect Record', description: 'Maintain a perfect return streak', category: 'achievement', requirement: 'Complete 25 consecutive on-time returns', icon: '💯' },
        { badgeType: 'CommunityBuilder', name: 'Community Builder', description: 'Grow the LendingLoop community', category: 'achievement', requirement: 'Invite 10 users who become active', icon: '🌟' }
      ])
    };

    const mockRouter = {
      navigate: jest.fn()
    };

    mockActivatedRoute = {
      snapshot: { params: {} },
      params: of({}),
      paramMap: paramMapSubject.asObservable()
    };

    mockLocation = {
      back: jest.fn()
    };

    await TestBed.configureTestingModule({
      imports: [ProfileComponent],
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: mockAuthService },
        { provide: UserService, useValue: mockUserService },
        { provide: NotificationService, useValue: mockNotificationService },
        { provide: ItemRequestService, useValue: mockItemRequestService },
        { provide: LoopScoreService, useValue: mockLoopScoreService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: Location, useValue: mockLocation }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load current user on init', () => {
    expect(component.currentUser).toEqual(mockUser);
    expect(mockAuthService.refreshCurrentUser).toHaveBeenCalled();
  });

  it('should display user name', () => {
    const compiled = fixture.nativeElement;
    const userName = compiled.querySelector('.user-name');
    expect(userName.textContent).toContain('Test User');
  });

  it('should display user email', () => {
    const compiled = fixture.nativeElement;
    const userEmail = compiled.querySelector('.user-email');
    expect(userEmail.textContent).toBe('test@example.com');
  });

  it('should pass earnedBadges to badge-display component', () => {
    //arrange
    const mockUserWithBadges: UserProfile = {
      ...mockUser,
      badges: [
        { badgeType: 'Bronze', awardedAt: new Date().toISOString() }
      ]
    };
    mockAuthService.getCurrentUser.mockReturnValue(of(mockUserWithBadges));
    mockAuthService.refreshCurrentUser.mockReturnValue(of(mockUserWithBadges));

    //act
    component.ngOnInit();
    fixture.detectChanges();

    //assert
    const badgeDisplay = fixture.nativeElement.querySelector('app-badge-display');
    expect(badgeDisplay).toBeTruthy();
  });

  it('should set showAllBadges to true for badge-display component', () => {
    //arrange & act
    fixture.detectChanges();

    //assert
    const badgeDisplay = fixture.nativeElement.querySelector('app-badge-display');
    expect(badgeDisplay).toBeTruthy();
    expect(badgeDisplay.getAttribute('ng-reflect-show-all-badges')).toBe('true');
  });

  it('should set isOwnProfile to true when no userId parameter', () => {
    //arrange
    paramMapSubject.next({
      get: jest.fn().mockReturnValue(null)
    });

    //act
    component.ngOnInit();

    //assert
    expect(component.isOwnProfile).toBe(true);
  });

  it('should set isOwnProfile to true when userId matches current user', () => {
    //arrange
    paramMapSubject.next({
      get: jest.fn((key: string) => key === 'userId' ? 'user123' : null)
    });

    //act
    component.ngOnInit();

    //assert
    expect(component.isOwnProfile).toBe(true);
    expect(component.userId).toBe('user123');
  });

  it('should set isOwnProfile to false when userId differs from current user', () => {
    //arrange
    paramMapSubject.next({
      get: jest.fn((key: string) => key === 'userId' ? 'otherUser456' : null)
    });

    //act
    component.ngOnInit();

    //assert
    expect(component.isOwnProfile).toBe(false);
    expect(component.userId).toBe('otherUser456');
  });

  it('should extract userId from route parameters', () => {
    //arrange
    paramMapSubject.next({
      get: jest.fn((key: string) => key === 'userId' ? 'testUser789' : null)
    });

    //act
    component.ngOnInit();

    //assert
    expect(component.userId).toBe('testUser789');
  });

  it('should initialize errorMessage as null', () => {
    //arrange & act
    // Component is already initialized in beforeEach

    //assert
    expect(component.errorMessage).toBeNull();
  });

  describe('loadProfile logic', () => {
    it('should initialize with loading state true', () => {
      //arrange
      // Create a new component without triggering ngOnInit
      const newFixture = TestBed.createComponent(ProfileComponent);
      const newComponent = newFixture.componentInstance;

      //act
      // Component is created but ngOnInit not called yet

      //assert
      expect(newComponent.isLoading).toBe(true);
    });

    it('should call authService.refreshCurrentUser when viewing own profile', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn().mockReturnValue(null)
      });
      mockAuthService.refreshCurrentUser.mockReturnValue(of(mockUser));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        expect(mockAuthService.refreshCurrentUser).toHaveBeenCalled();
        expect(component.profileData).toEqual(mockUser);
        expect(component.isLoading).toBe(false);
        done();
      }, 100);
    });

    it('should call userService.getPublicProfile when viewing another user profile', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn((key: string) => key === 'userId' ? 'otherUser456' : null)
      });
      mockUserService.getPublicProfile.mockReturnValue(of(mockPublicProfile));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        expect(mockUserService.getPublicProfile).toHaveBeenCalledWith('otherUser456');
        expect(component.profileData).toEqual(mockPublicProfile);
        expect(component.isLoading).toBe(false);
        done();
      }, 100);
    });

    it('should set profileData and isLoading false on successful private profile load', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn().mockReturnValue(null)
      });
      mockAuthService.refreshCurrentUser.mockReturnValue(of(mockUser));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        expect(component.profileData).toEqual(mockUser);
        expect(component.currentUser).toEqual(mockUser);
        expect(component.isLoading).toBe(false);
        expect(component.errorMessage).toBeNull();
        done();
      }, 100);
    });

    it('should set profileData and isLoading false on successful public profile load', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn((key: string) => key === 'userId' ? 'otherUser456' : null)
      });
      mockUserService.getPublicProfile.mockReturnValue(of(mockPublicProfile));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        expect(component.profileData).toEqual(mockPublicProfile);
        expect(component.isLoading).toBe(false);
        expect(component.errorMessage).toBeNull();
        done();
      }, 100);
    });

    it('should set error message on 403 error when loading public profile', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn((key: string) => key === 'userId' ? 'otherUser456' : null)
      });
      const error = { status: 403, error: { message: 'Forbidden' } };
      mockUserService.getPublicProfile.mockReturnValue(throwError(() => error));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        expect(component.errorMessage).toBe('You can only view profiles of users in your loops');
        expect(component.isLoading).toBe(false);
        done();
      }, 100);
    });

    it('should set error message on 404 error when loading public profile', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn((key: string) => key === 'userId' ? 'nonExistentUser' : null)
      });
      const error = { status: 404, error: { message: 'Not Found' } };
      mockUserService.getPublicProfile.mockReturnValue(throwError(() => error));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        expect(component.errorMessage).toBe('User not found');
        expect(component.isLoading).toBe(false);
        done();
      }, 100);
    });

    it('should set generic error message on other errors when loading public profile', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn((key: string) => key === 'userId' ? 'otherUser456' : null)
      });
      const error = { status: 500, error: { message: 'Internal Server Error' } };
      mockUserService.getPublicProfile.mockReturnValue(throwError(() => error));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        expect(component.errorMessage).toBe('Failed to load profile');
        expect(component.isLoading).toBe(false);
        done();
      }, 100);
    });

    it('should set error message when loading own profile fails', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn().mockReturnValue(null)
      });
      const error = { status: 500, error: { message: 'Internal Server Error' } };
      mockAuthService.refreshCurrentUser.mockReturnValue(throwError(() => error));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        expect(component.errorMessage).toBe('Failed to load profile');
        expect(component.isLoading).toBe(false);
        done();
      }, 100);
    });
  });

  describe('goBack', () => {
    it('should call location.back() when goBack is invoked', () => {
      //arrange
      // mockLocation is already set up in beforeEach

      //act
      component.goBack();

      //assert
      expect(mockLocation.back).toHaveBeenCalled();
    });
  });

  describe('page title display', () => {
    it('should display "My Profile" when viewing own profile', () => {
      //arrange
      paramMapSubject.next({
        get: jest.fn().mockReturnValue(null)
      });

      //act
      component.ngOnInit();
      fixture.detectChanges();

      //assert
      const pageTitle = fixture.nativeElement.querySelector('.page-title');
      expect(pageTitle).toBeTruthy();
      expect(pageTitle.textContent.trim()).toBe('My Profile');
    });

    it('should display "User Profile" when viewing another user profile', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn((key: string) => key === 'userId' ? 'otherUser456' : null)
      });

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        fixture.detectChanges();
        const pageTitle = fixture.nativeElement.querySelector('.page-title');
        expect(pageTitle).toBeTruthy();
        expect(pageTitle.textContent.trim()).toBe('User Profile');
        done();
      }, 100);
    });
  });

  describe('error message display', () => {
    it('should display error message when errorMessage is set', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn((key: string) => key === 'userId' ? 'otherUser456' : null)
      });
      const error = { status: 403, error: { message: 'Forbidden' } };
      mockUserService.getPublicProfile.mockReturnValue(throwError(() => error));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        fixture.detectChanges();
        const errorElement = fixture.nativeElement.querySelector('.error-message');
        expect(errorElement).toBeTruthy();
        expect(errorElement.textContent.trim()).toBe('You can only view profiles of users in your loops');
        done();
      }, 100);
    });

    it('should hide profile content when error message is present', (done) => {
      //arrange
      paramMapSubject.next({
        get: jest.fn((key: string) => key === 'userId' ? 'nonExistentUser' : null)
      });
      const error = { status: 404, error: { message: 'Not Found' } };
      mockUserService.getPublicProfile.mockReturnValue(throwError(() => error));

      //act
      component.ngOnInit();

      //assert
      setTimeout(() => {
        fixture.detectChanges();
        const profileHeader = fixture.nativeElement.querySelector('.profile-header');
        const profileContent = fixture.nativeElement.querySelector('.profile-content');
        expect(profileHeader).toBeFalsy();
        expect(profileContent).toBeFalsy();
        done();
      }, 100);
    });

    it('should not display error message when errorMessage is null', () => {
      //arrange
      paramMapSubject.next({
        get: jest.fn().mockReturnValue(null)
      });

      //act
      component.ngOnInit();
      fixture.detectChanges();

      //assert
      const errorElement = fixture.nativeElement.querySelector('.error-message');
      expect(errorElement).toBeFalsy();
    });

    it('should show profile content when no error message', () => {
      //arrange
      paramMapSubject.next({
        get: jest.fn().mockReturnValue(null)
      });

      //act
      component.ngOnInit();
      fixture.detectChanges();

      //assert
      const profileHeader = fixture.nativeElement.querySelector('.profile-header');
      const profileContent = fixture.nativeElement.querySelector('.profile-content');
      expect(profileHeader).toBeTruthy();
      expect(profileContent).toBeTruthy();
    });
  });
});
