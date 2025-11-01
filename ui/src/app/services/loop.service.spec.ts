import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { LoopService } from './loop.service';
import { Loop } from '../models/loop.interface';
import { LoopInvitation } from '../models/loop-invitation.interface';
import { LoopMember } from '../models/loop-member.interface';
import { SharedItem } from '../models/shared-item.interface';

describe('LoopService', () => {
  let service: LoopService;
  let httpMock: HttpTestingController;
  const API_URL = '/api/loops';

  const mockLoop: Loop = {
    id: 'loop1',
    name: 'Test Loop',
    creatorId: 'user1',
    memberIds: ['user1', 'user2'],
    createdAt: new Date('2024-01-01'),
    updatedAt: new Date('2024-01-02')
  };

  const mockLoopMember: LoopMember = {
    id: 'member1',
    email: 'member@example.com',
    firstName: 'John',
    lastName: 'Doe'
  };

  const mockInvitation: LoopInvitation = {
    id: 'inv1',
    loopId: 'loop1',
    loopName: 'Test Loop',
    invitedEmail: 'invited@example.com',
    invitedUserId: 'user2',
    invitedByUserId: 'user1',
    invitedByUserName: 'John Doe',
    invitationToken: 'token123',
    status: 'Pending',
    createdAt: new Date('2024-01-01'),
    expiresAt: new Date('2024-02-01')
  };

  const mockItem: SharedItem = {
    id: '1',
    name: 'Test Item',
    description: 'Test Description',
    userId: 'user1',
    isAvailable: true,
    visibleToLoopIds: ['loop1'],
    visibleToAllLoops: false,
    visibleToFutureLoops: false,
    createdAt: new Date('2024-01-01'),
    updatedAt: new Date('2024-01-02')
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [LoopService]
    });
    service = TestBed.inject(LoopService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    // Assert
    expect(service).toBeTruthy();
  });

  describe('createLoop', () => {
    it('should create loop successfully', (done) => {
      // Arrange
      const loopName = 'New Loop';

      // Act
      service.createLoop(loopName).subscribe(loop => {
        // Assert
        expect(loop).toEqual(mockLoop);
        done();
      });

      const req = httpMock.expectOne(API_URL);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ name: loopName });
      req.flush(mockLoop);
    });
  });

  describe('getUserLoops', () => {
    it('should fetch user loops successfully', (done) => {
      // Arrange
      const mockLoops: Loop[] = [mockLoop];

      // Act
      service.getUserLoops().subscribe(loops => {
        // Assert
        expect(loops).toEqual(mockLoops);
        expect(loops.length).toBe(1);
        done();
      });

      const req = httpMock.expectOne(API_URL);
      expect(req.request.method).toBe('GET');
      req.flush(mockLoops);
    });
  });

  describe('getLoopById', () => {
    it('should fetch loop by id successfully', (done) => {
      // Arrange
      const loopId = 'loop1';

      // Act
      service.getLoopById(loopId).subscribe(loop => {
        // Assert
        expect(loop).toEqual(mockLoop);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${loopId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockLoop);
    });
  });

  describe('getLoopMembers', () => {
    it('should fetch loop members successfully', (done) => {
      // Arrange
      const loopId = 'loop1';
      const mockMembers: LoopMember[] = [mockLoopMember];

      // Act
      service.getLoopMembers(loopId).subscribe(members => {
        // Assert
        expect(members).toEqual(mockMembers);
        expect(members.length).toBe(1);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${loopId}/members`);
      expect(req.request.method).toBe('GET');
      req.flush(mockMembers);
    });
  });

  describe('getLoopItems', () => {
    it('should fetch loop items without search', (done) => {
      // Arrange
      const loopId = 'loop1';
      const mockResponse = { items: [mockItem], totalCount: 1 };

      // Act
      service.getLoopItems(loopId).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        expect(response.items.length).toBe(1);
        expect(response.totalCount).toBe(1);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${loopId}/items`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should fetch loop items with search parameter', (done) => {
      // Arrange
      const loopId = 'loop1';
      const searchTerm = 'test';
      const mockResponse = { items: [mockItem], totalCount: 1 };

      // Act
      service.getLoopItems(loopId, searchTerm).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${loopId}/items?search=${encodeURIComponent(searchTerm)}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('inviteByEmail', () => {
    it('should invite user by email successfully', (done) => {
      // Arrange
      const loopId = 'loop1';
      const email = 'invited@example.com';

      // Act
      service.inviteByEmail(loopId, email).subscribe(invitation => {
        // Assert
        expect(invitation).toEqual(mockInvitation);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${loopId}/invite-email`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email });
      req.flush(mockInvitation);
    });
  });

  describe('inviteUser', () => {
    it('should invite user by userId successfully', (done) => {
      // Arrange
      const loopId = 'loop1';
      const userId = 'user2';

      // Act
      service.inviteUser(loopId, userId).subscribe(invitation => {
        // Assert
        expect(invitation).toEqual(mockInvitation);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${loopId}/invite-user`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ userId });
      req.flush(mockInvitation);
    });
  });

  describe('getPotentialInvitees', () => {
    it('should fetch potential invitees successfully', (done) => {
      // Arrange
      const loopId = 'loop1';
      const mockPotentialInvitees: LoopMember[] = [mockLoopMember];

      // Act
      service.getPotentialInvitees(loopId).subscribe(invitees => {
        // Assert
        expect(invitees).toEqual(mockPotentialInvitees);
        expect(invitees.length).toBe(1);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${loopId}/potential-invitees`);
      expect(req.request.method).toBe('GET');
      req.flush(mockPotentialInvitees);
    });
  });

  describe('acceptInvitationByToken', () => {
    it('should accept invitation by token successfully', (done) => {
      // Arrange
      const token = 'token123';

      // Act
      service.acceptInvitationByToken(token).subscribe(invitation => {
        // Assert
        expect(invitation).toEqual(mockInvitation);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/invitations/${token}/accept`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockInvitation);
    });
  });

  describe('acceptInvitationByUser', () => {
    it('should accept invitation by user successfully', (done) => {
      // Arrange
      const invitationId = 'inv1';

      // Act
      service.acceptInvitationByUser(invitationId).subscribe(invitation => {
        // Assert
        expect(invitation).toEqual(mockInvitation);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/invitations/${invitationId}/accept-user`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockInvitation);
    });
  });

  describe('getPendingInvitations', () => {
    it('should fetch pending invitations successfully', (done) => {
      // Arrange
      const mockInvitations: LoopInvitation[] = [mockInvitation];

      // Act
      service.getPendingInvitations().subscribe(invitations => {
        // Assert
        expect(invitations).toEqual(mockInvitations);
        expect(invitations.length).toBe(1);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/invitations/pending`);
      expect(req.request.method).toBe('GET');
      req.flush(mockInvitations);
    });
  });
});
