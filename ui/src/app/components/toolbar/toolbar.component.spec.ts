import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToolbarComponent } from './toolbar.component';
import { AuthService } from '../../services/auth.service';
import { ItemRequestService } from '../../services/item-request.service';
import { Router, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('ToolbarComponent', () => {
  let component: ToolbarComponent;
  let fixture: ComponentFixture<ToolbarComponent>;
  let mockAuthService: any;
  let mockItemRequestService: any;
  let mockRouter: any;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockAuthService = {
      getCurrentUser: jest.fn().mockReturnValue(of(null)),
      logout: jest.fn()
    };
    mockItemRequestService = {
      getPendingRequests: jest.fn().mockReturnValue(of([]))
    };
    mockRouter = {
      navigate: jest.fn()
    };
    mockActivatedRoute = {
      snapshot: { params: {} }
    };

    await TestBed.configureTestingModule({
      imports: [ToolbarComponent],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: ItemRequestService, useValue: mockItemRequestService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    fixture = TestBed.createComponent(ToolbarComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
