import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { LoopListComponent } from './loop-list.component';
import { LoopService } from '../../services/loop.service';
import { UserService } from '../../services/user.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { getMockToolbarServices } from '../../testing/mock-services';

describe('LoopListComponent', () => {
  let component: LoopListComponent;
  let fixture: ComponentFixture<LoopListComponent>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockUserService: jest.Mocked<UserService>;
  let mockRouter: jest.Mocked<Router>;

  const mockLoops: Loop[] = [
    {
      id: 'loop1',
      name: 'Test Loop 1',
      description: 'Test Description',
      creatorId: 'user1',
      memberIds: ['user1', 'user2'],
      isPublic: false,
      isArchived: false,
      ownershipHistory: [],
      createdAt: new Date(),
      updatedAt: new Date()
    },
    {
      id: 'loop2',
      name: 'Test Loop 2',
      description: 'Test Description',
      creatorId: 'user1',
      memberIds: ['user1'],
      isPublic: false,
      isArchived: false,
      ownershipHistory: [],
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  beforeEach(async () => {
    const toolbarMocks = getMockToolbarServices();

    mockLoopService = {
      getUserLoops: jest.fn(),
    } as any;

    mockUserService = {
      getCurrentUser: jest.fn().mockReturnValue(of(null)),
    } as any;

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    const activatedRouteMock = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [LoopListComponent, ToolbarComponent],
      providers: [
        provideHttpClient(),
        { provide: LoopService, useValue: mockLoopService },
        { provide: UserService, useValue: mockUserService },
        { provide: NotificationService, useValue: toolbarMocks.mockNotificationService },
        { provide: ItemRequestService, useValue: toolbarMocks.mockItemRequestService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoopListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load loops on init', (done) => {
    //arrange
    mockLoopService.getUserLoops.mockReturnValue(of(mockLoops));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getUserLoops).toHaveBeenCalled();
      expect(component.loops).toEqual(mockLoops);
      expect(component.loading).toBe(false);
      done();
    }, 0);
  });

  it('should handle error when loading loops fails', (done) => {
    //arrange
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    mockLoopService.getUserLoops.mockReturnValue(
      throwError(() => new Error('Failed to load'))
    );

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(component.loading).toBe(false);
      expect(component.loops).toEqual([]);
      expect(consoleErrorSpy).toHaveBeenCalledWith('Error loading loops:', expect.any(Error));
      consoleErrorSpy.mockRestore();
      done();
    }, 0);
  });

  it('should navigate to loop detail when navigateToLoop is called', () => {
    //arrange
    const loopId = 'loop1';

    //act
    component.navigateToLoop(loopId);

    //assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/loops', loopId]);
  });
});
