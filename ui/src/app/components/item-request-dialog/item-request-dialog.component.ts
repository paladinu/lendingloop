import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';

export interface ItemRequestDialogData {
    itemName: string;
}

export interface ItemRequestDialogResult {
    message: string;
    expectedReturnDate?: Date;
}

@Component({
    selector: 'app-item-request-dialog',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        MatDialogModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatDatepickerModule,
        MatNativeDateModule
    ],
    templateUrl: './item-request-dialog.component.html',
    styleUrls: ['./item-request-dialog.component.css']
})
export class ItemRequestDialogComponent {
    message = '';
    expectedReturnDate?: Date;
    readonly maxLength = 500;
    readonly minDate = new Date();

    constructor(
        public dialogRef: MatDialogRef<ItemRequestDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: ItemRequestDialogData
    ) {
        // Set minDate to tomorrow to prevent selecting today or past dates
        this.minDate.setDate(this.minDate.getDate() + 1);
    }

    get remainingCharacters(): number {
        return this.maxLength - this.message.length;
    }

    get isMessageTooLong(): boolean {
        return this.message.length > this.maxLength;
    }

    onCancel(): void {
        this.dialogRef.close();
    }

    onSubmit(): void {
        if (!this.isMessageTooLong) {
            this.dialogRef.close({ 
                message: this.message,
                expectedReturnDate: this.expectedReturnDate
            });
        }
    }
}
