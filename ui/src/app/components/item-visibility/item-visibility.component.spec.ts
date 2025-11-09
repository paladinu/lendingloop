import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { ItemVisibilityComponent } from './item-visibility.component';
import { ItemsService } from '../../services/items.service';
import { LoopService } from '../../services/loop.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { SharedItem } from '../../models/shared-item.interface';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { getMockToolbarServices } from '../../testing/mock-services';

describe('ItemVisibilityComponent', () => {
  let component: ItemVisibilityComponent;
  let fixture: ComponentFixture<ItemVisibilityComponent>;
  let mockItemsService: jest.Mocked<ItemsService>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockRouter: jest.Mocked<Router>;
  let mockActivatedRoute: any;

  const mockItem: SharedItem = {
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
  };

  const mockLoops: Loop[] = [
    {
      id: 'loop1',
      name: 'Test Loop',
      creatorId: 'user1',
      memberIds: ['user1'],
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  beforeEach(async () => {
    const toolbarMocks = getMockToolbarServices();

    mockItemsService = {
      getItemById: jest.fn(),
      updateItemVisibility: jest.fn(),
    } as any;

    mockLoopService = {
      getUserLoops: jest.fn(),
    } as any;

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jest.fn().mockReturnValue('item1')
        }
      },
      paramMap: of({
        get: jest.fn().mockReturnValue('item1')
      })
    };

    await TestBed.configureTestingModule({
      imports: [ItemVisibilityComponent, ToolbarComponent],
      providers: [
        provideHttpClient(),
        { provide: ItemsService, useValue: mockItemsService },
        { provide: LoopService, useValue: mockLoopService },
        { provide: NotificationService, useValue: toolbarMocks.mockNotificationService },
        { provide: ItemRequestService, useValue: toolbarMocks.mockItemRequestService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ItemVisibilityComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load item and loops on init', (done) => {
    //arrange
    mockItemsService.getItemById.mockReturnValue(of(mockItem));
    mockLoopService.getUserLoops.mockReturnValue(of(mockLoops));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockItemsService.getItemById).toHaveBeenCalledWith('item1');
      expect(mockLoopService.getUserLoops).toHaveBeenCalled();
      expect(component.item).toEqual(mockItem);
      expect(component.loops).toEqual(mockLoops);
      done();
    }, 0);
  });
});
