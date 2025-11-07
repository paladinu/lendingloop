import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PublicLoopsComponent } from './public-loops.component';
import { LoopService } from '../../services/loop.service';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

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
      imports: [PublicLoopsComponent, FormsModule],
      providers: [
        { provide: LoopService, useValue: mockLoopService },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .overrideComponent(PublicLoopsComponent, {
      remove: { imports: [ToolbarComponent] },
      add: { imports: [] }
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
    mockLoopService.getPublicLoops.mockReturnValue(
      throwError(() => new Error('Failed to load'))
    );

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(component.loading).toBe(false);
      expect(component.loops).toEqual([]);
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
