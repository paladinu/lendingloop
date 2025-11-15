import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProfileComponent } from './profile.component';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { LoopScoreService } from '../../services/loop-score.service';
import { provideHttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { UserProfile } from '../../models/auth.interface';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('ProfileComponent', () => {
  let component: ProfileComponent;
  let fixture: ComponentFixture<ProfileComponent>;
  let mockAuthService: any;

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

  beforeEach(async () => {
    mockAuthService = {
      getCurrentUser: jest.fn().mockReturnValue(of(mockUser)),
      refreshCurrentUser: jest.fn().mockReturnValue(of(mockUser))
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

    const mockActivatedRoute = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [ProfileComponent],
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: mockAuthService },
        { provide: NotificationService, useValue: mockNotificationService },
        { provide: ItemRequestService, useValue: mockItemRequestService },
        { provide: LoopScoreService, useValue: mockLoopScoreService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
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
});
