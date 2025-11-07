import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { LoopMembersComponent } from './loop-members.component';
import { LoopService } from '../../services/loop.service';
import { UserService } from '../../services/user.service';
import { Loop } from '../../models/loop.interface';
import { UserProfile } from '../../models/auth.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

describe('LoopMembersComponent', () => {
  let component: LoopMembersComponent;
  let fixture: ComponentFixture<LoopMembersComponent>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockUserService: jest.Mocked<UserService>;
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

  const mockMembers: UserProfile[] = [
    {
      id: 'user1',
      email: 'user1@example.com',
      firstName: 'John',
      lastName: 'Doe',
      streetAddress: '123 Main St',
      isEmailVerified: true
    },
    {
      id: 'user2',
      email: 'user2@example.com',
      firstName: 'Jane',
      lastName: 'Smith',
      streetAddress: '456 Oak Ave',
      isEmailVerified: true
    }
  ];

  beforeEach(async () => {
    mockLoopService = {
      getLoopById: jest.fn(),
      getLoopMembers: jest.fn(),
    } as any;

    mockUserService = {
      getCurrentUser: jest.fn(),
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
      imports: [LoopMembersComponent],
      providers: [
        { provide: LoopService, useValue: mockLoopService },
        { provide: UserService, useValue: mockUserService },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    })
    .overrideComponent(LoopMembersComponent, {
      remove: { imports: [ToolbarComponent] },
      add: { imports: [] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoopMembersComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load loop and members on init', (done) => {
    //arrange
    mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
    mockLoopService.getLoopMembers.mockReturnValue(of(mockMembers));
    mockUserService.getCurrentUser.mockReturnValue(of(mockMembers[0]));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getLoopById).toHaveBeenCalledWith('loop1');
      expect(mockLoopService.getLoopMembers).toHaveBeenCalledWith('loop1');
      expect(component.loop).toEqual(mockLoop);
      expect(component.members).toEqual(mockMembers);
      done();
    }, 0);
  });
});
