import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
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
      imports: [ItemCardComponent, HttpClientTestingModule]
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

  it('should emit editItem event when onEditItem is called', () => {
    //arrange
    component.item = mockItem;
    const editItemSpy = jest.spyOn(component.editItem, 'emit');

    //act
    component.onEditItem();

    //assert
    expect(editItemSpy).toHaveBeenCalledWith('1');
  });

  it('should return visible tags up to maximum limit', () => {
    //arrange
    component.item = {
      ...mockItem,
      tags: ['tag1', 'tag2', 'tag3', 'tag4', 'tag5', 'tag6', 'tag7']
    };

    //act
    const visibleTags = component.getVisibleTags();

    //assert
    expect(visibleTags).toEqual(['tag1', 'tag2', 'tag3', 'tag4', 'tag5']);
    expect(visibleTags.length).toBe(5);
  });

  it('should return all tags when less than maximum', () => {
    //arrange
    component.item = {
      ...mockItem,
      tags: ['tag1', 'tag2', 'tag3']
    };

    //act
    const visibleTags = component.getVisibleTags();

    //assert
    expect(visibleTags).toEqual(['tag1', 'tag2', 'tag3']);
    expect(visibleTags.length).toBe(3);
  });

  it('should return empty array when no tags', () => {
    //arrange
    component.item = {
      ...mockItem,
      tags: undefined
    };

    //act
    const visibleTags = component.getVisibleTags();

    //assert
    expect(visibleTags).toEqual([]);
  });

  it('should calculate hidden tag count correctly', () => {
    //arrange
    component.item = {
      ...mockItem,
      tags: ['tag1', 'tag2', 'tag3', 'tag4', 'tag5', 'tag6', 'tag7', 'tag8']
    };

    //act
    const hiddenCount = component.getHiddenTagCount();

    //assert
    expect(hiddenCount).toBe(3); // 8 total - 5 visible = 3 hidden
  });

  it('should return zero hidden count when tags are within limit', () => {
    //arrange
    component.item = {
      ...mockItem,
      tags: ['tag1', 'tag2', 'tag3']
    };

    //act
    const hiddenCount = component.getHiddenTagCount();

    //assert
    expect(hiddenCount).toBe(0);
  });

  it('should return zero hidden count when no tags', () => {
    //arrange
    component.item = {
      ...mockItem,
      tags: undefined
    };

    //act
    const hiddenCount = component.getHiddenTagCount();

    //assert
    expect(hiddenCount).toBe(0);
  });

  it('should check if item has tags', () => {
    //arrange & act & assert
    component.item = { ...mockItem, tags: ['tag1', 'tag2'] };
    expect(component.hasTags()).toBe(true);

    component.item = { ...mockItem, tags: [] };
    expect(component.hasTags()).toBe(false);

    component.item = { ...mockItem, tags: undefined };
    expect(component.hasTags()).toBe(false);
  });

  it('should emit tagClick event when tag is clicked', () => {
    //arrange
    const tagClickSpy = jest.spyOn(component.tagClick, 'emit');

    //act
    component.onTagClick('electronics');

    //assert
    expect(tagClickSpy).toHaveBeenCalledWith('electronics');
  });
});
