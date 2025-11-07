import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoopInviteComponent } from './loop-invite.component';
import { LoopService } from '../../services/loop.service';
import { ToolbarComponent } from '../toolbar/toolbar.component';

describe('LoopInviteComponent', () => {
  let component: LoopInviteComponent;
  let fixture: ComponentFixture<LoopInviteComponent>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockLoopService = {
      inviteByEmail: jest.fn(),
      inviteUser: jest.fn(),
      getPotentialInvitees: jest.fn(),
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
      imports: [LoopInviteComponent, FormsModule],
      providers: [
        { provide: LoopService, useValue: mockLoopService },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    })
    .overrideComponent(LoopInviteComponent, {
      remove: { imports: [ToolbarComponent] },
      add: { imports: [] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoopInviteComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load potential invitees on init', (done) => {
    //arrange
    const mockUsers = [
      { id: 'user1', email: 'user1@example.com', firstName: 'John', lastName: 'Doe' }
    ];
    mockLoopService.getPotentialInvitees.mockReturnValue(of(mockUsers));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getPotentialInvitees).toHaveBeenCalledWith('loop1');
      expect(component.potentialInvitees).toEqual(mockUsers);
      done();
    }, 0);
  });

  it('should invite by email successfully', (done) => {
    //arrange
    mockLoopService.getPotentialInvitees.mockReturnValue(of([]));
    mockLoopService.inviteByEmail.mockReturnValue(of({ message: 'Invitation sent' } as any));
    component.email = 'test@example.com';

    fixture.detectChanges();

    //act
    component.onEmailInvite();

    //assert
    setTimeout(() => {
      expect(mockLoopService.inviteByEmail).toHaveBeenCalledWith('loop1', 'test@example.com');
      expect(component.email).toBe('');
      done();
    }, 0);
  });
});
