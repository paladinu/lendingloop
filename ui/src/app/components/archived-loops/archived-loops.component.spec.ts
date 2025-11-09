import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { ArchivedLoopsComponent } from './archived-loops.component';
import { LoopService } from '../../services/loop.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { getMockToolbarServices } from '../../testing/mock-services';

describe('ArchivedLoopsComponent', () => {
  let component: ArchivedLoopsComponent;
  let fixture: ComponentFixture<ArchivedLoopsComponent>;
  let mockLoopService: jest.Mocked<LoopService>;

  const mockArchivedLoops: Loop[] = [
    {
      id: 'loop1',
      name: 'Archived Loop 1',
      description: '',
      creatorId: 'user1',
      memberIds: ['user1'],
      isPublic: false,
      isArchived: true,
      archivedAt: new Date(),
      ownershipHistory: [],
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  beforeEach(async () => {
    const toolbarMocks = getMockToolbarServices();

    mockLoopService = {
      getArchivedLoops: jest.fn(),
      restoreLoop: jest.fn(),
    } as any;

    const activatedRouteMock = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [ArchivedLoopsComponent, ToolbarComponent],
      providers: [
        provideHttpClient(),
        { provide: LoopService, useValue: mockLoopService },
        { provide: NotificationService, useValue: toolbarMocks.mockNotificationService },
        { provide: ItemRequestService, useValue: toolbarMocks.mockItemRequestService },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ArchivedLoopsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load archived loops on init', (done) => {
    //arrange
    mockLoopService.getArchivedLoops.mockReturnValue(of(mockArchivedLoops));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getArchivedLoops).toHaveBeenCalled();
      expect(component.loops).toEqual(mockArchivedLoops);
      expect(component.loading).toBe(false);
      done();
    }, 0);
  });
});
