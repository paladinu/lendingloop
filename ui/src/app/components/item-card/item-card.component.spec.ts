import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ItemCardComponent } from './item-card.component';
import { SharedItem } from '../../models/shared-item.interface';
import { Loop } from '../../models/loop.interface';

describe('ItemCardComponent', () => {
  let component: ItemCardComponent;
  let fixture: ComponentFixture<ItemCardComponent>;

  const mockItem: SharedItem = {
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
  };

  const mockLoops: Loop[] = [
    { id: 'loop1', name: 'Loop 1', creatorId: 'user123', memberIds: ['user123'], createdAt: new Date(), updatedAt: new Date(), memberCount: 2 }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ItemCardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ItemCardComponent);
    component = fixture.componentInstance;
    component.item = mockItem;
    component.loops = mockLoops;
    fixture.detectChanges();
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should show edit button only for item owners', () => {
    //arrange
    component.isOwner = true;
    fixture.detectChanges();

    //act
    const compiled = fixture.nativeElement;
    const editButton = compiled.querySelector('.edit-item-btn');

    //assert
    expect(editButton).toBeTruthy();
  });

  it('should not show edit button for non-owners', () => {
    //arrange
    component.isOwner = false;
    fixture.detectChanges();

    //act
    const compiled = fixture.nativeElement;
    const editButton = compiled.querySelector('.edit-item-btn');

    //assert
    expect(editButton).toBeFalsy();
  });

  it('should emit editItem event when edit button is clicked', () => {
    //arrange
    component.isOwner = true;
    fixture.detectChanges();
    const emitSpy = jest.spyOn(component.editItem, 'emit');

    //act
    component.onEditItem();

    //assert
    expect(emitSpy).toHaveBeenCalledWith('1');
  });

  it('should emit editVisibility event when visibility button is clicked', () => {
    //arrange
    component.isOwner = true;
    fixture.detectChanges();
    const emitSpy = jest.spyOn(component.editVisibility, 'emit');

    //act
    component.onEditVisibility();

    //assert
    expect(emitSpy).toHaveBeenCalledWith('1');
  });
});
