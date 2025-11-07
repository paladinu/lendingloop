import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ItemAddComponent } from './item-add.component';
import { ItemsService } from '../../services/items.service';
import { LoopService } from '../../services/loop.service';
import { SharedItem } from '../../models/shared-item.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

describe('ItemAddComponent', () => {
  let component: ItemAddComponent;
  let fixture: ComponentFixture<ItemAddComponent>;
  let mockItemsService: jest.Mocked<ItemsService>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockRouter: jest.Mocked<Router>;

  beforeEach(async () => {
    mockItemsService = {
      createItem: jest.fn(),
      uploadItemImage: jest.fn(),
    } as any;

    mockLoopService = {
      getUserLoops: jest.fn(),
    } as any;

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    const activatedRouteMock = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [ItemAddComponent, FormsModule],
      providers: [
        provideRouter([
          { path: 'loops/create', component: ItemAddComponent },
          { path: 'loops/invitations', component: ItemAddComponent }
        ]),
        { provide: ItemsService, useValue: mockItemsService },
        { provide: LoopService, useValue: mockLoopService },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .overrideComponent(ItemAddComponent, {
      remove: { imports: [ToolbarComponent] },
      add: { imports: [] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(ItemAddComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should create item successfully', (done) => {
    //arrange
    const mockItem: SharedItem = {
      id: 'item1',
      name: 'Test Item',
      description: 'Test Description',
      userId: 'user1',
      isAvailable: true,
      visibleToLoopIds: [],
      visibleToAllLoops: true,
      visibleToFutureLoops: true,
      createdAt: new Date(),
      updatedAt: new Date()
    };
    mockItemsService.createItem.mockReturnValue(of(mockItem));
    mockLoopService.getUserLoops.mockReturnValue(of([]));
    component.newItemName = 'Test Item';
    component.newItemDescription = 'Test Description';

    fixture.detectChanges();

    //act
    component.addItem();

    //assert
    setTimeout(() => {
      expect(mockItemsService.createItem).toHaveBeenCalled();
      expect(component.success).toBeTruthy();
      done();
    }, 100);
  });
});
