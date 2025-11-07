import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoopSettingsComponent } from './loop-settings.component';
import { LoopService } from '../../services/loop.service';
import { Loop } from '../../models/loop.interface';

describe('LoopSettingsComponent', () => {
  let component: LoopSettingsComponent;
  let fixture: ComponentFixture<LoopSettingsComponent>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockRouter: jest.Mocked<Router>;
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

  beforeEach(async () => {
    mockLoopService = {
      getLoopSettings: jest.fn(),
      getLoopById: jest.fn(),
      updateLoopSettings: jest.fn(),
    } as any;

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jest.fn().mockReturnValue('loop1')
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [LoopSettingsComponent, FormsModule, HttpClientTestingModule],
      providers: [
        { provide: LoopService, useValue: mockLoopService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoopSettingsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load loop settings on init', (done) => {
    //arrange
    mockLoopService.getLoopSettings.mockReturnValue(of({
      name: mockLoop.name,
      description: mockLoop.description,
      isPublic: mockLoop.isPublic
    }));
    mockLoopService.getLoopById.mockReturnValue(of(mockLoop));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(mockLoopService.getLoopSettings).toHaveBeenCalledWith('loop1');
      expect(component.settings.name).toBe(mockLoop.name);
      expect(component.settings.description).toBe(mockLoop.description);
      expect(component.settings.isPublic).toBe(mockLoop.isPublic);
      expect(component.loading).toBe(false);
      done();
    }, 0);
  });

  it('should handle error when loading settings fails', (done) => {
    //arrange
    mockLoopService.getLoopSettings.mockReturnValue(
      throwError(() => ({ error: { message: 'Failed to load' } }))
    );
    mockLoopService.getLoopById.mockReturnValue(of(mockLoop));

    //act
    fixture.detectChanges();

    //assert
    setTimeout(() => {
      expect(component.loading).toBe(false);
      expect(component.message).toContain('Failed to load settings');
      done();
    }, 0);
  });

  it('should update loop settings successfully', (done) => {
    //arrange
    mockLoopService.getLoopSettings.mockReturnValue(of({
      name: mockLoop.name,
      description: mockLoop.description,
      isPublic: mockLoop.isPublic
    }));
    mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
    mockLoopService.updateLoopSettings.mockReturnValue(of(mockLoop));

    fixture.detectChanges();

    component.settings = {
      name: 'Updated Loop',
      description: 'Updated Description',
      isPublic: true
    };

    //act
    component.saveSettings();

    //assert
    setTimeout(() => {
      expect(mockLoopService.updateLoopSettings).toHaveBeenCalledWith(
        'loop1',
        component.settings
      );
      expect(component.saving).toBe(false);
      expect(component.message).toContain('saved successfully');
      done();
    }, 0);
  });

  it('should not save when name is empty', () => {
    //arrange
    mockLoopService.getLoopSettings.mockReturnValue(of({
      name: mockLoop.name,
      description: mockLoop.description,
      isPublic: mockLoop.isPublic
    }));
    mockLoopService.getLoopById.mockReturnValue(of(mockLoop));

    fixture.detectChanges();

    component.settings.name = '';

    //act
    component.saveSettings();

    //assert
    expect(mockLoopService.updateLoopSettings).not.toHaveBeenCalled();
    expect(component.message).toContain('required');
  });

  it('should handle error when updating settings fails', (done) => {
    //arrange
    mockLoopService.getLoopSettings.mockReturnValue(of({
      name: mockLoop.name,
      description: mockLoop.description,
      isPublic: mockLoop.isPublic
    }));
    mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
    mockLoopService.updateLoopSettings.mockReturnValue(
      throwError(() => ({ error: { message: 'Update failed' } }))
    );

    fixture.detectChanges();

    //act
    component.saveSettings();

    //assert
    setTimeout(() => {
      expect(component.saving).toBe(false);
      expect(component.message).toContain('Failed to save');
      done();
    }, 0);
  });
});
