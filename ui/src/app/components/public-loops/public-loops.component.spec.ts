import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { PublicLoopsComponent } from './public-loops.component';
import { LoopService } from '../../services/loop.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { getMockToolbarServices } from '../../testing/mock-services';

describe('PublicLoopsComponent', () => {
  let component: PublicLoopsComponent;
  let fixture: ComponentFixture<PublicLoopsComponent>;
  let mockLoopService: jest.Mocked<LoopService>;

  const mockLoops: Loop[] = [
    {
      id: 'loop1',
      name: 'Public Loop 1',
      description: 'A public loop',
      creatorId: 'user1',
      memberIds: ['user1', 'user2'],
      isPublic: true,
      isArchived: false,
      ownershipHistory: [],
      createdAt: new Date(),
      updatedAt: new Date()
    },
    {
      id: 'loop2',
      name: 'Public Loop 2',
      description: 'Another public loop',
      creatorId: 'user2',
      memberIds: ['user2'],
      isPublic: true,
      isArchived: false,
      ownershipHistory: [],
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  beforeEach(async () => {
    const toolbarMocks = getMockToolbarServices();

    mockLoopService = {
      getPublicLoops: jest.fn(),
      searchPublicLoops: jest.fn(),
      getMyJoinRequests: jest.fn().mockReturnValue(of([])),
    } as any;

    const activatedRouteMock = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [PublicLoopsComponent, FormsModule, ToolbarComponent],
      providers: [
        provideHttpClient(),
        { provide: LoopService, useValue: mockLoopService },
        { provide: NotificationService, useValue: toolbarMocks.mockNotificationService },
        { provide: ItemRequestService, useValue: toolbarMocks.mockItemRequestService },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PublicLoopsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load public loops on init', (done) => {
    //arrange
    mockLoopService.getPublicLoops.mockReturnValue(of(mockLoops));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getPublicLoops).toHaveBeenCalled();
      expect(component.loops).toEqual(mockLoops);
      expect(component.loading).toBe(false);
      done();
    }, 0);
  });

  it('should handle error when loading public loops fails', (done) => {
    //arrange
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    mockLoopService.getPublicLoops.mockReturnValue(
      throwError(() => new Error('Failed to load'))
    );

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(component.loading).toBe(false);
      expect(component.loops).toEqual([]);
      consoleErrorSpy.mockRestore();
      done();
    }, 0);
  });

  it('should search public loops when search term is provided', (done) => {
    //arrange
    const searchTerm = 'test';
    mockLoopService.searchPublicLoops.mockReturnValue(of([mockLoops[0]]));

    //act
    component.searchTerm = searchTerm;
    component.onSearch();

    //assert
    setTimeout(() => {
      expect(mockLoopService.searchPublicLoops).toHaveBeenCalledWith(searchTerm, 0, 20);
      expect(component.loops).toEqual([mockLoops[0]]);
      expect(component.loading).toBe(false);
      done();
    }, 0);
  });

  it('should load all loops when search term is empty', (done) => {
    //arrange
    mockLoopService.getPublicLoops.mockReturnValue(of(mockLoops));

    //act
    component.searchTerm = '';
    component.onSearch();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getPublicLoops).toHaveBeenCalledWith(0, 20);
      expect(component.loops).toEqual(mockLoops);
      done();
    }, 0);
  });
});
