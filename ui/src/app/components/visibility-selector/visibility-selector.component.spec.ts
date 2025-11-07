import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { VisibilitySelectorComponent } from './visibility-selector.component';
import { Loop } from '../../models/loop.interface';

describe('VisibilitySelectorComponent', () => {
  let component: VisibilitySelectorComponent;
  let fixture: ComponentFixture<VisibilitySelectorComponent>;

  const mockLoops: Loop[] = [
    {
      id: 'loop1',
      name: 'Test Loop 1',
      creatorId: 'user1',
      memberIds: ['user1'],
      createdAt: new Date(),
      updatedAt: new Date()
    },
    {
      id: 'loop2',
      name: 'Test Loop 2',
      creatorId: 'user1',
      memberIds: ['user1'],
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VisibilitySelectorComponent, FormsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(VisibilitySelectorComponent);
    component = fixture.componentInstance;
    component.loops = mockLoops;
    fixture.detectChanges();
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should initialize with provided loops', () => {
    //assert
    expect(component.loops).toEqual(mockLoops);
  });

  it('should emit selection changes', (done) => {
    //arrange
    component.selectionChange.subscribe((selection) => {
      //assert
      expect(selection.selectedLoopIds).toContain('loop1');
      done();
    });

    //act
    component.toggleLoop('loop1');
  });
});
