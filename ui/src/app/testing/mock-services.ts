import { of } from 'rxjs';

/**
 * Creates mock services for ToolbarComponent dependencies
 * This prevents HTTP calls during tests that include ToolbarComponent
 */
export function getMockToolbarServices() {
  return {
    mockNotificationService: {
      getUnreadCount: jest.fn().mockReturnValue(of(0)),
      getNotifications: jest.fn().mockReturnValue(of([])),
      markAsRead: jest.fn().mockReturnValue(of({})),
      markAllAsRead: jest.fn().mockReturnValue(of(true)),
      deleteNotification: jest.fn().mockReturnValue(of(true)),
    },
    mockItemRequestService: {
      getPendingRequests: jest.fn().mockReturnValue(of([])),
      getRequestById: jest.fn().mockReturnValue(of(null)),
      createRequest: jest.fn().mockReturnValue(of({})),
      approveRequest: jest.fn().mockReturnValue(of({})),
      rejectRequest: jest.fn().mockReturnValue(of({})),
      cancelRequest: jest.fn().mockReturnValue(of({})),
      completeRequest: jest.fn().mockReturnValue(of({})),
    }
  };
}

/**
 * Sets up localStorage to prevent token parsing errors in tests
 * Call this in beforeEach before creating components that use AuthService
 */
export function setupTestLocalStorage() {
  // Clear localStorage before each test
  localStorage.clear();
  
  // Optionally set a valid mock token if needed
  // This prevents "Error parsing token" console errors
  const mockToken = createMockJwtToken();
  localStorage.setItem('auth_token', mockToken);
}

/**
 * Creates a mock JWT token with a far-future expiration
 * This prevents token expiration errors in tests
 */
function createMockJwtToken(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(JSON.stringify({
    sub: 'test-user-id',
    email: 'test@example.com',
    exp: Math.floor(Date.now() / 1000) + 3600 // Expires in 1 hour
  }));
  const signature = 'mock-signature';
  return `${header}.${payload}.${signature}`;
}
