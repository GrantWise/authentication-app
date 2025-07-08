# Product Requirements Document: JWT Token Authentication

## 1. Problem Statement & Objectives

### Current State
The application currently uses basic password encryption stored in SQL tables without OAuth or JWT implementation. Authentication lacks modern security standards and compliance requirements.

### Problem Statement
- Current authentication system is inadequate and not secure by 2025 standards
- No stateless authentication support for mobile and web applications
- Non-compliance with ISO 27001 requirements demanded by clients
- Lack of industry-standard authentication practices

### Objectives
- Implement secure JWT-based authentication following 2025 best practices
- Achieve ISO 27001 compliance for authentication processes
- Enable stateless authentication for both mobile and web applications
- Deliver pragmatic security implementation without over-engineering

### Success Metrics
- Login authentication completes in under 500ms
- Backend access requires valid JWT token (no unauthorized access)
- ISO 27001 compliance verification for authentication module
- Support for both mobile and web application authentication flows

## 2. User Stories & Requirements

### Core User Stories
**As a mobile web app user, I want to...**
- Log in with my username and password to access the application
- Stay logged in for 60 minutes without re-authentication
- Use the same account on multiple devices simultaneously
- Log out securely and have my session invalidated

**As a PC user (flow creator/admin), I want to...**
- Log in with my username and password to access flow creation tools
- Access administrative functions based on my role permissions
- Stay logged in for 60 minutes while working on flows
- Log out securely when finished

**As an administrator, I want to...**
- Configure password complexity requirements for all users
- Manage user authentication settings
- Monitor authentication events for security purposes

### Functional Requirements
- [ ] User authentication with username/password
- [ ] JWT token generation and validation
- [ ] 60-minute session duration with automatic expiry
- [ ] Multi-device concurrent session support
- [ ] Role-based access control (mobile users, flow creators, admins)
- [ ] Configurable password complexity rules (admin-defined)
- [ ] Multi-Factor Authentication (MFA) - optional/configurable by admin
- [ ] Rate limiting: Maximum 5 login attempts per account within 15 minutes
- [ ] Account lockout: Automatic 30-minute lockout after 5 failed attempts
- [ ] Audit logging for failed authentication attempts
- [ ] Secure logout with token invalidation
- [ ] Session timeout handling
- [ ] Password reset self-service functionality

### MFA Support
- [ ] Time-based One-Time Password (TOTP) applications (Google Authenticator, Authy)
- [ ] SMS verification codes
- [ ] Email verification codes
- [ ] Backup codes for account recovery

### Non-Functional Requirements
- **Security**: ISO 27001 compliant authentication, bcrypt/Argon2 password hashing, HTTPS enforcement
- **Performance**: Login response time under 500ms, support for 500 initial users with 100 concurrent authentications
- **Scalability**: Support multiple simultaneous sessions per user, designed for horizontal scaling
- **Availability**: 99.9% authentication service uptime, reliable authentication for both mobile and PC platforms

## 3. Technical Specifications

### JWT Implementation
- **Token Type**: Bearer tokens
- **Signing Algorithm**: RS256 (RSA with SHA-256) - 2025 best practice for asymmetric signing
- **Token Storage**: sessionStorage (client-side)
- **Backend Framework**: .NET (ASP.NET Core)
- **JWT Library**: Microsoft.AspNetCore.Authentication.JwtBearer

### Token Structure
**Access Token (15-minute lifespan):**
```json
{
  "header": {
    "alg": "RS256",
    "typ": "JWT",
    "kid": "key-id"
  },
  "payload": {
    "sub": "[user_id]",
    "username": "[username]",
    "email": "[user_email]",
    "roles": "[user_roles]",
    "iat": "[issued_at]",
    "exp": "[expiration_15_min]",
    "jti": "[jwt_id]",
    "iss": "[application_issuer]",
    "aud": "[application_audience]"
  }
}
```

**Refresh Token (60-minute lifespan):**
```json
{
  "header": {
    "alg": "RS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "[user_id]",
    "token_type": "refresh",
    "iat": "[issued_at]",
    "exp": "[expiration_60_min]",
    "jti": "[refresh_jwt_id]"
  }
}
```

### API Endpoints
- `POST /api/auth/login` - User authentication
- `POST /api/auth/refresh` - Token refresh
- `POST /api/auth/logout` - User logout
- `POST /api/auth/logout-all` - Logout from all devices
- `GET /api/auth/verify` - Token validation
- `POST /api/auth/mfa/setup` - MFA configuration
- `POST /api/auth/mfa/verify` - MFA verification

### Database Schema (SQL Server)
**Users Table:**
- UserId (uniqueidentifier, PK)
- Username (nvarchar(255), unique)
- Email (nvarchar(255))
- PasswordHash (nvarchar(255))
- Salt (nvarchar(255))
- Roles (nvarchar(500))
- MfaEnabled (bit)
- MfaSecret (nvarchar(255), encrypted)
- IsLocked (bit)
- LockoutEnd (datetime2)
- FailedLoginAttempts (int)
- LastLoginAttempt (datetime2)
- CreatedAt (datetime2)
- UpdatedAt (datetime2)

**ActiveSessions Table:**
- SessionId (uniqueidentifier, PK)
- UserId (uniqueidentifier, FK)
- RefreshTokenJti (nvarchar(255))
- DeviceInfo (nvarchar(500))
- IpAddress (nvarchar(45))
- CreatedAt (datetime2)
- ExpiresAt (datetime2)

**AuditLog Table:**
- LogId (bigint, PK, Identity)
- UserId (uniqueidentifier)
- Username (nvarchar(255))
- EventType (nvarchar(100)) -- 'LOGIN_FAILED', 'LOGIN_SUCCESS', 'LOGOUT', etc.
- IpAddress (nvarchar(45))
- UserAgent (nvarchar(1000))
- Timestamp (datetime2)
- Details (nvarchar(max))

### Session Storage Strategy (Hybrid)
- **SQL Server**: Persistent storage for refresh tokens and session metadata
- **In-Memory Cache**: Fast lookup for active sessions and rate limiting
- **Session Cleanup**: Background service to remove expired sessions

### Security Configuration
- **Key Management**: RSA 2048-bit key pairs stored securely on-premises
- **Key Rotation**: Quarterly key rotation with graceful transition
- **Token Expiration**: 
  - Access Token: 15 minutes
  - Refresh Token: 60 minutes
- **Rate Limiting**: 5 attempts per username per 15 minutes
- **Account Lockout**: 30 minutes after 5 failed attempts
- **Password Hashing**: bcrypt with minimum 12 rounds

### .NET Implementation Components
```csharp
// Authentication middleware configuration
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Services
- IAuthenticationService
- IJwtTokenService  
- IUserService
- IAuditService
- ISessionManager
- IMfaService
```

### On-Premises Infrastructure
- **Web Server**: IIS or Kestrel on Windows Server
- **Database**: SQL Server (on-premises instance)
- **Caching**: Redis on local network or SQL Server memory-optimized tables
- **Load Balancing**: Application Request Routing (ARR) if multiple servers
- **SSL/TLS**: On-premises certificate management

## 4. User Experience

### Login Interface
- **Layout**: Single page with username and password fields
- **Branding**: Company logo prominently displayed at top
- **Styling**: Consistent with company brand guidelines
- **Fields**: 
  - Username (text input)
  - Password (password input with show/hide toggle)
  - Login button
- **Additional Elements**: "Forgot Password?" link below login button

### MFA Setup Flow (First-time setup)
1. **Post-login prompt**: After successful initial login, display inline message: "Secure your account with Multi-Factor Authentication"
2. **Setup wizard**: 
   - Step 1: Choose MFA method (TOTP app recommended, SMS/Email as alternatives)
   - Step 2: QR code display for TOTP apps with manual entry code as backup
   - Step 3: Verification - user enters first TOTP code to confirm setup
   - Step 4: Backup codes generation and display (printable/downloadable)
3. **Completion**: "MFA successfully configured" confirmation

### MFA Login Flow (When enabled)
1. **Initial authentication**: Standard username/password validation
2. **MFA challenge**: Inline prompt "Enter your authentication code" appears below password field
3. **Code input**: Single text field accepting TOTP, SMS, or backup codes
4. **Alternative methods**: "Having trouble?" expandable section with:
   - "Send SMS code" button
   - "Send email code" button  
   - "Use backup code" option
5. **Verification**: Seamless transition to application on successful MFA

### Error Handling
- **Invalid Username**: "Username not found. Please check your username and try again."
- **Incorrect Password**: "Incorrect password. Please try again."
- **Account Locked**: "Account temporarily locked due to multiple failed attempts. Please try again in 30 minutes or contact support."
- **MFA Code Invalid**: "Invalid authentication code. Please try again."
- **Session Expired**: "Your session has expired. Please log in again."
- **Rate Limited**: "Too many login attempts. Please wait 15 minutes before trying again."

### Session Management Experience
#### Timeout Warnings
- **5-minute warning**: Modal popup "Your session will expire in 5 minutes. Continue working?" with "Stay Logged In" and "Logout" buttons
- **1-minute warning**: Updated modal "Your session will expire in 1 minute" with countdown timer
- **Expiration**: Automatic redirect to login page with message "Session expired. Please log in again."

#### Active Sessions Management
- **Access**: "Manage Sessions" link in user profile/settings menu
- **Session List Display**:
  ```
  Active Sessions
  ┌─────────────────────────────────────────────────┐
  │ 🖥️ Desktop - Chrome (Current Session)          │
  │ Location: Office Network                        │
  │ Last Activity: 2 minutes ago                    │
  │ [Logout This Session]                           │
  ├─────────────────────────────────────────────────┤
  │ 📱 Mobile - Safari                              │
  │ Location: Mobile Network                        │
  │ Last Activity: 15 minutes ago                   │
  │ [Logout This Session]                           │
  └─────────────────────────────────────────────────┘
  
  [Logout All Other Sessions]  [Logout All Sessions]
  ```

### Password Reset Flow
1. **Initiation**: Click "Forgot Password?" link on login page
2. **Email input**: "Enter your email address" with submit button
3. **Confirmation**: "Password reset link sent to your email" message
4. **Email content**: Secure link with expiration (15 minutes)
5. **Reset page**: New password + confirm password fields with complexity requirements shown
6. **Completion**: "Password successfully reset. Please log in with your new password."

### Cross-Platform Consistency
- **Responsive Design**: Identical interface adapts to screen size
- **Touch Optimization**: Appropriately sized buttons and inputs for mobile
- **Keyboard Navigation**: Full keyboard support for accessibility
- **Performance**: Optimized loading for both mobile and desktop connections

### Visual Feedback
- **Loading States**: Spinner during authentication process
- **Success Indicators**: Green checkmarks for successful actions
- **Progress Indicators**: Step indicators for multi-step processes (MFA setup)
- **Field Validation**: Real-time feedback for password complexity requirements

## 5. Security & Compliance

### Token Management
- **Access Token Expiration**: [e.g., 15 minutes]
- **Refresh Token Expiration**: [e.g., 7 days]
- **Token Rotation**: [Refresh token rotation policy]

### Security Headers
- Content Security Policy (CSP)
- Strict-Transport-Security
- X-Content-Type-Options
- X-Frame-Options

### Best Practices
- Password hashing with bcrypt/scrypt
- Rate limiting on authentication attempts
- Audit logging for authentication events
- Input validation and sanitization

## 6. Success Criteria

### Acceptance Criteria
- [ ] Users can successfully log in with valid credentials
- [ ] Invalid login attempts are properly rejected
- [ ] Tokens expire and refresh as configured
- [ ] Users can log out and tokens are invalidated
- [ ] Authentication state persists across browser sessions
- [ ] Security headers are properly implemented

### Performance Benchmarks
- Login response time: [< X ms]
- Token validation time: [< X ms]
- Concurrent user capacity: [X users]

### Security Validation
- [ ] Penetration testing completed
- [ ] Security audit passed
- [ ] Compliance requirements met
- [ ] No sensitive data in JWT payload