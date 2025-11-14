import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ItemRequestDialogData {
    itemName: string;
}

export interface ItemRequestDialogResult {
    message: string;
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
        MatInputModule
    ],
    templateUrl: './item-request-dialog.component.html',
    styleUrls: ['./item-request-dialog.component.css']
})
export class ItemRequestDialogComponent {
    message = '';
    readonly maxLength = 500;

    constructor(
        public dialogRef: MatDialogRef<ItemRequestDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: ItemRequestDialogData
    ) { }

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
            this.dialogRef.close({ message: this.message });
        }
    }
}
