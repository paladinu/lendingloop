import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ItemRequestDialogComponent, ItemRequestDialogData } from './item-request-dialog.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('ItemRequestDialogComponent', () => {
    let component: ItemRequestDialogComponent;
    let fixture: ComponentFixture<ItemRequestDialogComponent>;
    let dialogRef: any;
    const mockDialogData: ItemRequestDialogData = {
        itemName: 'Test Item'
    };

    beforeEach(async () => {
        const dialogRefSpy = {
            close: jest.fn()
        };

        await TestBed.configureTestingModule({
            imports: [ItemRequestDialogComponent],
            providers: [
                { provide: MatDialogRef, useValue: dialogRefSpy },
                { provide: MAT_DIALOG_DATA, useValue: mockDialogData }
            ],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        dialogRef = TestBed.inject(MatDialogRef);
        fixture = TestBed.createComponent(ItemRequestDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        //arrange
        //act
        //assert
        expect(component).toBeTruthy();
    });

    it('should initialize with empty message', () => {
        //arrange
        //act
        //assert
        expect(component.message).toBe('');
    });

    it('should display item name in dialog data', () => {
        //arrange
        //act
        //assert
        expect(component.data.itemName).toBe('Test Item');
    });

    it('should calculate remaining characters correctly', () => {
        //arrange
        component.message = 'Hello';

        //act
        const remaining = component.remainingCharacters;

        //assert
        expect(remaining).toBe(495); // 500 - 5
    });

    it('should enforce 500 character limit', () => {
        //arrange
        component.message = 'a'.repeat(501);

        //act
        const isTooLong = component.isMessageTooLong;

        //assert
        expect(isTooLong).toBe(true);
    });

    it('should allow messages up to 500 characters', () => {
        //arrange
        component.message = 'a'.repeat(500);

        //act
        const isTooLong = component.isMessageTooLong;

        //assert
        expect(isTooLong).toBe(false);
    });

    it('should close dialog without result when cancelled', () => {
        //arrange
        //act
        component.onCancel();

        //assert
        expect(dialogRef.close).toHaveBeenCalledWith();
    });

    it('should close dialog with message when submitted', () => {
        //arrange
        component.message = 'I need this for a project';

        //act
        component.onSubmit();

        //assert
        expect(dialogRef.close).toHaveBeenCalledWith({ 
            message: 'I need this for a project',
            expectedReturnDate: undefined
        });
    });

    it('should not submit when message is too long', () => {
        //arrange
        component.message = 'a'.repeat(501);

        //act
        component.onSubmit();

        //assert
        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('should submit with empty message', () => {
        //arrange
        component.message = '';

        //act
        component.onSubmit();

        //assert
        expect(dialogRef.close).toHaveBeenCalledWith({ message: '', expectedReturnDate: undefined });
    });

    it('should initialize with minDate set to tomorrow', () => {
        //arrange
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);

        //act
        const minDate = component.minDate;

        //assert
        expect(minDate.getDate()).toBe(tomorrow.getDate());
    });

    it('should submit with expectedReturnDate when provided', () => {
        //arrange
        const futureDate = new Date('2026-12-31');
        component.message = 'Test message';
        component.expectedReturnDate = futureDate;

        //act
        component.onSubmit();

        //assert
        expect(dialogRef.close).toHaveBeenCalledWith({ 
            message: 'Test message',
            expectedReturnDate: futureDate
        });
    });

    it('should submit without expectedReturnDate when not provided', () => {
        //arrange
        component.message = 'Test message';
        component.expectedReturnDate = undefined;

        //act
        component.onSubmit();

        //assert
        expect(dialogRef.close).toHaveBeenCalledWith({ 
            message: 'Test message',
            expectedReturnDate: undefined
        });
    });

    it('should submit with both message and expectedReturnDate', () => {
        //arrange
        const futureDate = new Date('2026-12-31');
        component.message = 'I need this for a week';
        component.expectedReturnDate = futureDate;

        //act
        component.onSubmit();

        //assert
        expect(dialogRef.close).toHaveBeenCalledWith({ 
            message: 'I need this for a week',
            expectedReturnDate: futureDate
        });
    });
});
