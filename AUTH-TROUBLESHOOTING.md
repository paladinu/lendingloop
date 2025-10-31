# Authentication Troubleshooting Guide

## Issue: 401 Response with No Error Message

If you're getting a 401 response when trying to log in with correct credentials, the most likely cause is that **the user's email has not been verified**.

## Why This Happens

1. When you register a new user, the system sends a verification email
2. The user must click the verification link in the email before they can log in
3. If email is not configured (SMTP settings), the verification email won't be sent
4. Without email verification, login attempts return a 403 status (which may appear as 401)

## Quick Fix: Manually Verify a User (Development Only)

### Option 1: Use PowerShell Script

```powershell
.\verify-user.ps1 -Email "your-email@example.com"
```

### Option 2: Use HTTP Request

**Check User Status:**
```http
GET http://localhost:8080/api/auth/dev/user-status/your-email@example.com
```

**Manually Verify User:**
```http
POST http://localhost:8080/api/auth/dev/verify-user
Content-Type: application/json

{
  "email": "your-email@example.com"
}
```

### Option 3: Use curl

**Check status:**
```bash
curl http://localhost:8080/api/auth/dev/user-status/your-email@example.com
```

**Verify user:**
```bash
curl -X POST http://localhost:8080/api/auth/dev/verify-user \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"your-email@example.com\"}"
```

## Improvements Made

### Backend Changes

1. **Enhanced Logging** - Added detailed logging to AuthController to track:
   - Login attempts
   - User lookup results
   - Password verification results
   - Email verification status

2. **Development Endpoints** - Added two new endpoints (Development only):
   - `GET /api/auth/dev/user-status/{email}` - Check user status
   - `POST /api/auth/dev/verify-user` - Manually verify a user's email

### Frontend Changes

1. **Better Error Handling** - Updated login component to:
   - Extract error messages from API responses
   - Display specific error messages for 401 and 403 status codes
   - Show generic error message for other failures

2. **Enhanced Logging** - Added console logging to track:
   - Login attempts
   - API responses
   - Authentication state changes

## How to Test

1. **Start the API** (from `/api` directory):
   ```bash
   dotnet run
   ```

2. **Start the Angular app** (from `/ui` directory):
   ```bash
   npm start
   ```

3. **Register a new user** at http://localhost:4200/register

4. **Verify the user** using one of the methods above

5. **Log in** at http://localhost:4200/login

## Checking Logs

### Backend Logs
Watch the console where you ran `dotnet run`. You should see:
- "Login attempt for email: ..."
- "User found for email: ..."
- "Password verification result: ..."

### Frontend Logs
Open browser DevTools (F12) and check the Console tab. You should see:
- "AuthService - attempting login for: ..."
- "AuthService - login successful" or error details

## Common Issues

### Issue: User not found
**Solution:** Make sure you registered the user first

### Issue: Invalid password
**Solution:** Check that you're using the correct password (must meet requirements: 8+ chars, uppercase, lowercase, special character)

### Issue: Email not verified
**Solution:** Use the manual verification endpoint or script

### Issue: CORS errors
**Solution:** Make sure the API is running on port 8080 and Angular on port 4200

## Token Not Being Sent to API

If you're getting a token back from login but it's not being sent on subsequent API calls:

### What Was Fixed

The issue was with the HTTP interceptor registration. Angular 19 uses functional interceptors instead of class-based interceptors.

**Changes made:**
1. Converted `AuthInterceptor` from a class to a functional interceptor
2. Updated `app.config.ts` to use `withInterceptors([authInterceptor])`
3. Added logging to track token attachment

### Verify Token is Working

Open browser DevTools (F12) and check the Console. You should see:
- `AuthService - storing auth data` (after login)
- `AuthService - token stored, can retrieve: true`
- `AuthInterceptor - Adding token to request: http://localhost:8080/api/items`

Check the Network tab:
1. Find the request to `/api/items`
2. Click on it
3. Look at the Request Headers
4. You should see: `Authorization: Bearer eyJhbGc...`

## Production Setup

In production, you should:
1. Configure SMTP settings in `appsettings.json`
2. Set up a real email service (Gmail, SendGrid, etc.)
3. The development endpoints will not be available in production
4. Users will receive real verification emails
