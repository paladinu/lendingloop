import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { AcceptInvitationComponent } from './accept-invitation.component';
import { LoopService } from '../../services/loop.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { getMockToolbarServices } from '../../testing/mock-services';

describe('AcceptInvitationComponent', () => {
  let component: AcceptInvitationComponent;
  let fixture: ComponentFixture<AcceptInvitationComponent>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockRouter: jest.Mocked<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const toolbarMocks = getMockToolbarServices();

    mockLoopService = {
      acceptInvitationByToken: jest.fn(),
    } as any;

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    mockActivatedRoute = {
      snapshot: {
        queryParamMap: {
          get: jest.fn().mockReturnValue('token123')
        }
      },
      queryParamMap: of({
        get: jest.fn().mockReturnValue('token123')
      })
    };

    await TestBed.configureTestingModule({
      imports: [AcceptInvitationComponent, ToolbarComponent],
      providers: [
        provideHttpClient(),
        { provide: LoopService, useValue: mockLoopService },
        { provide: NotificationService, useValue: toolbarMocks.mockNotificationService },
        { provide: ItemRequestService, useValue: toolbarMocks.mockItemRequestService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AcceptInvitationComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should accept invitation successfully', (done) => {
    //arrange
    mockLoopService.acceptInvitationByToken.mockReturnValue(of({ loopId: 'loop1' } as any));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.acceptInvitationByToken).toHaveBeenCalledWith('token123');
      expect(component.success).toBe(true);
      expect(component.loading).toBe(false);
      done();
    }, 0);
  });

  it('should handle error when accepting invitation fails', (done) => {
    //arrange
    mockLoopService.acceptInvitationByToken.mockReturnValue(
      throwError(() => ({ error: { message: 'Invalid token' } }))
    );

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(component.success).toBe(false);
      expect(component.loading).toBe(false);
      done();
    }, 0);
  });
});
