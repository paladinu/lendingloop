import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ItemRequestService } from '../../services/item-request.service';
import { ItemRequest, RequestStatus } from '../../models/item-request.interface';

@Component({
    selector: 'app-my-requests',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './my-requests.component.html',
    styleUrls: ['./my-requests.component.css']
})
export class MyRequestsComponent implements OnInit {
    allRequests: ItemRequest[] = [];
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

        this.itemRequestService.getMyRequests().subscribe({
            next: (requests) => {
                this.allRequests = requests;
                this.isLoading = false;
            },
            error: (error) => {
                this.errorMessage = error.message || 'Failed to load requests';
                this.isLoading = false;
            }
        });
    }

    onCancel(request: ItemRequest): void {
        if (!request.id) {
            return;
        }

        this.errorMessage = '';
        this.successMessage = '';

        this.itemRequestService.cancelRequest(request.id).subscribe({
            next: () => {
                this.successMessage = 'Request cancelled successfully';
                this.loadRequests();
            },
            error: (error) => {
                this.errorMessage = error.message || 'Failed to cancel request';
            }
        });
    }

    get pendingRequests(): ItemRequest[] {
        return this.allRequests.filter(r => r.status === RequestStatus.Pending);
    }

    get approvedRequests(): ItemRequest[] {
        return this.allRequests.filter(r => r.status === RequestStatus.Approved);
    }

    get completedRequests(): ItemRequest[] {
        return this.allRequests.filter(r => r.status === RequestStatus.Completed);
    }

    get rejectedRequests(): ItemRequest[] {
        return this.allRequests.filter(r => r.status === RequestStatus.Rejected);
    }

    get cancelledRequests(): ItemRequest[] {
        return this.allRequests.filter(r => r.status === RequestStatus.Cancelled);
    }

    get hasRequests(): boolean {
        return this.allRequests.length > 0;
    }

    getStatusBadgeClass(status: RequestStatus): string {
        switch (status) {
            case RequestStatus.Pending:
                return 'bg-warning';
            case RequestStatus.Approved:
                return 'bg-success';
            case RequestStatus.Rejected:
                return 'bg-danger';
            case RequestStatus.Cancelled:
                return 'bg-secondary';
            case RequestStatus.Completed:
                return 'bg-info';
            default:
                return 'bg-secondary';
        }
    }
}
