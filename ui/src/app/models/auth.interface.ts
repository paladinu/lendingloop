export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    streetAddress: string;
}

export interface AuthResponse {
    token: string;
    user: UserProfile;
    expiresAt: string;
}

export interface RegisterResponse {
    message: string;
    user: UserProfile;
}

export interface UserProfile {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    streetAddress: string;
    isEmailVerified: boolean;
    loopScore: number;
}

export interface VerifyEmailRequest {
    token: string;
}

export interface VerificationResponse {
    message: string;
    success: boolean;
}

export interface ScoreHistoryEntry {
    timestamp: string;
    points: number;
    actionType: 'BorrowCompleted' | 'OnTimeReturn' | 'LendApproved' | 'LendCancelled';
    itemRequestId: string;
    itemName: string;
}

export type ScoreActionType = 'BorrowCompleted' | 'OnTimeReturn' | 'LendApproved' | 'LendCancelled';