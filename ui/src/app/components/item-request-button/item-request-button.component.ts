import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ItemRequestService } from '../../services/item-request.service';
import { ItemRequest, RequestStatus } from '../../models/item-request.interface';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-item-request-button',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './item-request-button.component.html',
    styleUrls: ['./item-request-button.component.css']
})
export class ItemRequestButtonComponent implements OnInit, OnDestroy {
    @Input() itemId!: string;
    @Input() ownerId!: string;
    @Output() requestCreated = new EventEmitter<ItemRequest>();

    existingRequest: ItemRequest | null = null;
    isLoading = false;
    errorMessage = '';
    currentUserId: string | null = null;

    RequestStatus = RequestStatus;
    
    private subscriptions = new Subscription();

    constructor(
        private itemRequestService: ItemRequestService,
        private authService: AuthService
    ) { }

    ngOnInit(): void {
        const userSub = this.authService.getCurrentUser().subscribe(user => {
            this.currentUserId = user?.id || null;
            this.loadExistingRequest();
        });
        this.subscriptions.add(userSub);
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    loadExistingRequest(): void {
        if (!this.itemId) {
            return;
        }

        const requestSub = this.itemRequestService.getRequestsForItem(this.itemId).subscribe({
            next: (requests) => {
                // Find any pending or approved request from current user
                this.existingRequest = requests.find(r =>
                    r.requesterId === this.currentUserId &&
                    (r.status === RequestStatus.Pending || r.status === RequestStatus.Approved)
                ) || null;
            },
            error: () => {
                // Silently handle error - component will show default state
            }
        });
        this.subscriptions.add(requestSub);
    }

    onRequestItem(): void {
        if (!this.itemId || this.isLoading || this.existingRequest) {
            return;
        }

        this.isLoading = true;
        this.errorMessage = '';

        this.itemRequestService.createRequest(this.itemId).subscribe({
            next: (request) => {
                this.existingRequest = request;
                this.requestCreated.emit(request);
                this.isLoading = false;
            },
            error: (error) => {
                this.errorMessage = error.message || 'Failed to create request';
                this.isLoading = false;
            }
        });
    }

    get buttonText(): string {
        if (this.existingRequest) {
            if (this.existingRequest.status === RequestStatus.Pending) {
                return 'Pending Request';
            } else if (this.existingRequest.status === RequestStatus.Approved) {
                return 'Currently Borrowed';
            }
        }
        return 'Request Item';
    }

    get isButtonDisabled(): boolean {
        return this.isLoading || !!this.existingRequest || this.currentUserId === this.ownerId;
    }

    get showButton(): boolean {
        return this.currentUserId !== this.ownerId;
    }
}
