import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { LoopInvitationsComponent } from './loop-invitations.component';
import { LoopService } from '../../services/loop.service';
import { LoopInvitation } from '../../models/loop-invitation.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

describe('LoopInvitationsComponent', () => {
  let component: LoopInvitationsComponent;
  let fixture: ComponentFixture<LoopInvitationsComponent>;
  let mockLoopService: jest.Mocked<LoopService>;

  const mockInvitations: LoopInvitation[] = [
    {
      id: 'inv1',
      loopId: 'loop1',
      loopName: 'Test Loop',
      invitedByUserId: 'user1',
      invitedByUserName: 'John Doe',
      invitedEmail: 'test@example.com',
      invitationToken: 'token123',
      status: 'Pending',
      createdAt: new Date(),
      expiresAt: new Date(Date.now() + 86400000)
    }
  ];

  beforeEach(async () => {
    mockLoopService = {
      getPendingInvitations: jest.fn(),
      acceptInvitationByUser: jest.fn(),
    } as any;

    const activatedRouteMock = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [LoopInvitationsComponent],
      providers: [
        { provide: LoopService, useValue: mockLoopService },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .overrideComponent(LoopInvitationsComponent, {
      remove: { imports: [ToolbarComponent] },
      add: { imports: [] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoopInvitationsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load pending invitations on init', (done) => {
    //arrange
    mockLoopService.getPendingInvitations.mockReturnValue(of(mockInvitations));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getPendingInvitations).toHaveBeenCalled();
      expect(component.invitations).toEqual(mockInvitations);
      expect(component.loading).toBe(false);
      done();
    }, 0);
  });
});
