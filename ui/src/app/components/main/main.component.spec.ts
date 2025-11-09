import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { MainComponent } from './main.component';
import { ItemsService } from '../../services/items.service';
import { AuthService } from '../../services/auth.service';
import { LoopService } from '../../services/loop.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { SharedItem } from '../../models/shared-item.interface';
import { UserProfile } from '../../models/auth.interface';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { getMockToolbarServices } from '../../testing/mock-services';

describe('MainComponent', () => {
  let component: MainComponent;
  let fixture: ComponentFixture<MainComponent>;
  let itemsService: jest.Mocked<ItemsService>;
  let authService: jest.Mocked<AuthService>;
  let loopService: jest.Mocked<LoopService>;
  let router: jest.Mocked<Router>;

  const mockUser: UserProfile = {
    id: 'user123',
    email: 'test@example.com',
    firstName: 'John',
    lastName: 'Doe',
    streetAddress: '123 Main St',
    isEmailVerified: true
  };

  const mockItems: SharedItem[] = [
    {
      id: '1',
      name: 'Test Item',
      description: 'Test Description',
      userId: 'user123',
      isAvailable: true,
      imageUrl: 'http://example.com/image.jpg',
      visibleToLoopIds: ['loop1'],
      visibleToAllLoops: false,
      visibleToFutureLoops: false,
      createdAt: new Date(),
      updatedAt: new Date(),
      ownerName: 'John Doe'
    }
  ];

  const mockLoops: Loop[] = [
    { id: 'loop1', name: 'Loop 1', creatorId: 'user123', memberIds: ['user123'], createdAt: new Date(), updatedAt: new Date(), memberCount: 2 }
  ];

  beforeEach(async () => {
    const toolbarMocks = getMockToolbarServices();

    const itemsServiceMock = {
      getItems: jest.fn()
    } as unknown as jest.Mocked<ItemsService>;

    const authServiceMock = {
      getCurrentUser: jest.fn()
    } as unknown as jest.Mocked<AuthService>;

    const loopServiceMock = {
      getUserLoops: jest.fn()
    } as unknown as jest.Mocked<LoopService>;

    const routerMock = {
      navigate: jest.fn()
    } as unknown as jest.Mocked<Router>;

    const activatedRouteMock = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [MainComponent, ToolbarComponent],
      providers: [
        provideHttpClient(),
        { provide: ItemsService, useValue: itemsServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: LoopService, useValue: loopServiceMock },
        { provide: NotificationService, useValue: toolbarMocks.mockNotificationService },
        { provide: ItemRequestService, useValue: toolbarMocks.mockItemRequestService },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MainComponent);
    component = fixture.componentInstance;
    itemsService = TestBed.inject(ItemsService) as jest.Mocked<ItemsService>;
    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    loopService = TestBed.inject(LoopService) as jest.Mocked<LoopService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;

    authService.getCurrentUser.mockReturnValue(of(mockUser));
    itemsService.getItems.mockReturnValue(of(mockItems));
    loopService.getUserLoops.mockReturnValue(of(mockLoops));
    
    // Prevent automatic change detection to avoid triggering child component lifecycle
    fixture.autoDetectChanges(false);
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
    jest.clearAllMocks();
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should navigate to edit route when onEditItem is called', () => {
    //arrange
    const itemId = '1';

    //act
    component.onEditItem(itemId);

    //assert
    expect(router.navigate).toHaveBeenCalledWith(['/items', itemId, 'edit']);
  });

  it('should navigate to visibility route when onEditVisibility is called', () => {
    //arrange
    const itemId = '1';

    //act
    component.onEditVisibility(itemId);

    //assert
    expect(router.navigate).toHaveBeenCalledWith(['/items', itemId, 'visibility']);
  });

  it('should identify item owner correctly', () => {
    //arrange
    component.currentUser = mockUser;
    const ownedItem = mockItems[0];
    const notOwnedItem = { ...mockItems[0], userId: 'differentUser' };

    //act
    const isOwner1 = component.isItemOwner(ownedItem);
    const isOwner2 = component.isItemOwner(notOwnedItem);

    //assert
    expect(isOwner1).toBe(true);
    expect(isOwner2).toBe(false);
  });
});
