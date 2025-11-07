import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { LoopCreateComponent } from './loop-create.component';
import { LoopService } from '../../services/loop.service';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

describe('LoopCreateComponent', () => {
  let component: LoopCreateComponent;
  let fixture: ComponentFixture<LoopCreateComponent>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockRouter: jest.Mocked<Router>;

  beforeEach(async () => {
    mockLoopService = {
      createLoop: jest.fn(),
    } as any;

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    const activatedRouteMock = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [LoopCreateComponent, FormsModule],
      providers: [
        { provide: LoopService, useValue: mockLoopService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .overrideComponent(LoopCreateComponent, {
      remove: { imports: [ToolbarComponent] },
      add: { imports: [] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoopCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should create loop successfully', (done) => {
    //arrange
    const mockLoop: Loop = {
      id: 'loop1',
      name: 'New Loop',
      description: '',
      creatorId: 'user1',
      memberIds: ['user1'],
      isPublic: false,
      isArchived: false,
      ownershipHistory: [],
      createdAt: new Date(),
      updatedAt: new Date()
    };
    mockLoopService.createLoop.mockReturnValue(of(mockLoop));
    component.loopName = 'New Loop';

    //act
    component.onSubmit();

    //assert
    setTimeout(() => {
      expect(mockLoopService.createLoop).toHaveBeenCalledWith('New Loop');
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/loops', 'loop1']);
      done();
    }, 0);
  });
});
