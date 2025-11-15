import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ItemRequestService } from '../../services/item-request.service';
import { ItemRequest, RequestStatus } from '../../models/item-request.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { LoopScoreDisplayComponent } from '../loop-score-display/loop-score-display.component';

@Component({
    selector: 'app-item-request-list',
    standalone: true,
    imports: [CommonModule, ToolbarComponent, LoopScoreDisplayComponent],
    templateUrl: './item-request-list.component.html',
    styleUrls: ['./item-request-list.component.css']
})
export class ItemRequestListComponent implements OnInit {
    pendingRequests: ItemRequest[] = [];
    approvedRequests: ItemRequest[] = [];
    isLoading = false;
    errorMessage = '';
    successMessage = '';

    RequestStatus = RequestStatus;

    constructor(private itemRequestService: ItemRequestService) { }

    ngOnInit(): void {
        this.loadRequests();
    }

    loadRequests(): void {
        this.isLoading = true;
        this.errorMessage = '';

        this.itemRequestService.getPendingRequests().subscribe({
            next: (requests) => {
                this.pendingRequests = requests.filter(r => r.status === RequestStatus.Pending);
                this.approvedRequests = requests.filter(r => r.status === RequestStatus.Approved);
                this.isLoading = false;
            },
            error: (error) => {
                this.errorMessage = error.message || 'Failed to load requests';
                this.isLoading = false;
            }
        });
    }

    onApprove(request: ItemRequest): void {
        if (!request.id) {
            return;
        }

        this.errorMessage = '';
        this.successMessage = '';

        this.itemRequestService.approveRequest(request.id).subscribe({
            next: () => {
                this.successMessage = 'Request approved successfully';
                this.loadRequests();
            },
            error: (error) => {
                this.errorMessage = error.message || 'Failed to approve request';
            }
        });
    }

    onReject(request: ItemRequest): void {
        if (!request.id) {
            return;
        }

        this.errorMessage = '';
        this.successMessage = '';

        this.itemRequestService.rejectRequest(request.id).subscribe({
            next: () => {
                this.successMessage = 'Request rejected successfully';
                this.loadRequests();
            },
            error: (error) => {
                this.errorMessage = error.message || 'Failed to reject request';
            }
        });
    }

    onComplete(request: ItemRequest): void {
        if (!request.id) {
            return;
        }

        this.errorMessage = '';
        this.successMessage = '';

        this.itemRequestService.completeRequest(request.id).subscribe({
            next: () => {
                this.successMessage = 'Request completed successfully';
                this.loadRequests();
            },
            error: (error) => {
                this.errorMessage = error.message || 'Failed to complete request';
            }
        });
    }

    get hasPendingRequests(): boolean {
        return this.pendingRequests.length > 0;
    }

    get hasApprovedRequests(): boolean {
        return this.approvedRequests.length > 0;
    }

    get hasAnyRequests(): boolean {
        return this.hasPendingRequests || this.hasApprovedRequests;
    }
}
