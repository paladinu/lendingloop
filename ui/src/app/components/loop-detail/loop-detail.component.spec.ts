import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { LoopDetailComponent } from './loop-detail.component';
import { LoopService } from '../../services/loop.service';
import { Loop } from '../../models/loop.interface';
import { SharedItem } from '../../models/shared-item.interface';
import { AuthService } from '../../services/auth.service';

describe('LoopDetailComponent', () => {
  let component: LoopDetailComponent;
  let fixture: ComponentFixture<LoopDetailComponent>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockAuthService: jest.Mocked<AuthService>;
  let mockActivatedRoute: any;

  const mockLoop: Loop = {
    id: 'loop1',
    name: 'Test Loop',
    description: 'Test Description',
    creatorId: 'user1',
    memberIds: ['user1', 'user2'],
    isPublic: false,
    isArchived: false,
    ownershipHistory: [],
    createdAt: new Date(),
    updatedAt: new Date()
  };

  const mockItems: SharedItem[] = [
    {
      id: 'item1',
      name: 'Test Item',
      description: 'Test Description',
      userId: 'user1',
      isAvailable: true,
      visibleToLoopIds: ['loop1'],
      visibleToAllLoops: false,
      visibleToFutureLoops: false,
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  beforeEach(async () => {
    mockLoopService = {
      getLoopById: jest.fn(),
      getLoopItems: jest.fn(),
    } as any;

    mockAuthService = {
      getCurrentUserId: jest.fn().mockReturnValue('user1'),
      getCurrentUser: jest.fn().mockReturnValue(of({ id: 'user1', email: 'test@example.com', firstName: 'Test', lastName: 'User', streetAddress: '123 Test St', isEmailVerified: true })),
    } as any;

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jest.fn().mockReturnValue('loop1')
        }
      },
      paramMap: of({
        get: jest.fn().mockReturnValue('loop1')
      })
    };

    await TestBed.configureTestingModule({
      imports: [LoopDetailComponent],
      providers: [
        provideHttpClient(),
        { provide: LoopService, useValue: mockLoopService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoopDetailComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load loop and items on init', (done) => {
    //arrange
    mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
    mockLoopService.getLoopItems.mockReturnValue(of({ items: mockItems, totalCount: 1 }));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getLoopById).toHaveBeenCalledWith('loop1');
      expect(mockLoopService.getLoopItems).toHaveBeenCalledWith('loop1');
      expect(component.loop).toEqual(mockLoop);
      expect(component.items).toEqual(mockItems);
      done();
    }, 0);
  });
});
