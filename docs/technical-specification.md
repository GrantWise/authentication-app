# JWT Token Authentication - Technical Specification

## 1. Technical Overview

### 1.1 Technical Vision and Objectives

**Vision**: Implement a pragmatic, secure JWT-based authentication system that meets ISO 27001 compliance requirements while maintaining the simplicity and performance needed for mid-sized manufacturing environments.

**Technical Objectives**:
- Replace basic password encryption with industry-standard JWT authentication
- Achieve sub-500ms authentication response times
- Support concurrent multi-device sessions for shared warehouse scanners
- Enable stateless authentication for horizontal scaling
- Maintain on-premises deployment compatibility

### 1.2 Document Scope and Audience

**Scope**: This specification defines the technical architecture, data models, and implementation requirements for JWT-based authentication across TransLution's mobile and desktop applications.

**Audience**: 
- Development teams implementing the authentication system
- Security teams validating compliance requirements
- DevOps teams managing deployment and infrastructure
- QA teams developing test strategies

### 1.3 Architecture Philosophy

**Core Principles**:
- **Start Simple**: Direct DbContext usage, add complexity only when measured
- **Vertical Slices**: Feature-based organization, not technical layers
- **Pragmatic Security**: Industry standards without over-engineering
- **Performance First**: Optimize for warehouse floor conditions
- **YAGNI Applied**: Build for current needs with clear extension points

### 1.4 Reference to PRD and Design Specifications

This specification implements:
- **PRD Requirements**: 60-minute sessions, multi-device support, ISO 27001 compliance
- **UI/UX Requirements**: Glove-friendly PIN entry, high-contrast displays, sub-10 second mobile login
- **Coding Standards**: Adherence to established patterns from `docs/ai-coding-instructions.md`

## 2. System Architecture Confirmation

### 2.1 Default Architecture Pattern Applicability

**Vertical Slice Architecture**: ✅ Confirmed
- Authentication as a vertical feature slice
- Self-contained authentication module with all layers
- Clear boundaries with other system features

**CQRS with MediatR**: ✅ Confirmed
- Command: Login, Logout, RefreshToken
- Query: ValidateToken, GetUserSessions
- Clear separation of read/write operations

### 2.2 Feature-Based Organization Structure

```
Features/
├── Authentication/
│   ├── Commands/
│   │   ├── Login/
│   │   ├── Logout/
│   │   ├── RefreshToken/
│   │   └── SetupMfa/
│   ├── Queries/
│   │   ├── ValidateToken/
│   │   └── GetActiveSessions/
│   ├── Models/
│   ├── Services/
│   └── Middleware/
```

### 2.3 System Context Diagram

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Mobile Apps   │     │   Desktop Apps  │     │   Admin Portal  │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                        │
         └───────────────────────┴────────────────────────┘
                                 │
                         ┌───────▼────────┐
                         │ Load Balancer  │
                         └───────┬────────┘
                                 │
                    ┌────────────┴────────────┐
                    │                         │
            ┌───────▼────────┐       ┌───────▼────────┐
            │ Auth Service 1 │       │ Auth Service 2 │
            └───────┬────────┘       └───────┬────────┘
                    │                         │
                    └────────────┬────────────┘
                                 │
                         ┌───────▼────────┐
                         │  SQL Server    │
                         │  (Sessions DB) │
                         └────────────────┘
```

### 2.4 Component Architecture (Vertical Slices)

| Component | Responsibility | Communication |
|-----------|---------------|---------------|
| Authentication Module | JWT generation, validation, session management | Direct DB access via EF Core |
| User Management | User data, roles, permissions | Shared domain models |
| Audit Service | Authentication event logging | Event-driven via MediatR |
| Session Manager | Active session tracking, cleanup | Background service |
| MFA Service | TOTP generation, email delivery | External provider integration |

### 2.5 Service Boundaries and Communication

**Internal Communication**:
- MediatR for intra-module communication
- Domain events for cross-module notifications
- No direct service-to-service calls

**External Communication**:
- RESTful API for client applications
- JWT tokens for stateless authentication
- Rate limiting at API gateway level

### 2.6 Data Flow Architecture

**Authentication Flow**:
1. Client → API Gateway (rate limiting)
2. API Gateway → Authentication Controller
3. Controller → MediatR Command Handler
4. Handler → User Repository (validation)
5. Handler → JWT Service (token generation)
6. Handler → Session Repository (tracking)
7. Response → Client (tokens + user data)

### 2.7 Scaling Architecture (Start Simple Approach)

**Phase 1 - Current Implementation**:
- Single SQL Server database
- In-process session caching
- Synchronous processing
- 500 concurrent users target

**Phase 2 - Future Scaling Points** (When Measured):
- Redis cache for session data (at 1000+ users)
- Read replicas for session queries (at 5000+ users)
- Microservice extraction (at 10000+ users)

## 3. Technology Stack Confirmation

### 3.1 Default Stack Applicability Assessment

| Technology | Default Choice | Project Fit | Justification |
|------------|---------------|-------------|---------------|
| Backend Framework | .NET 8 + ASP.NET Core | ✅ Confirmed | Enterprise-ready, on-premises compatible |
| Database | SQL Server | ✅ Confirmed | Existing infrastructure, meets performance needs |
| ORM | Entity Framework Core | ✅ Confirmed | Simplified data access, migration support |
| Authentication | JWT RS256 | ✅ Confirmed | Industry standard, asymmetric signing |
| Logging | Serilog | ✅ Confirmed | Structured logging for compliance |
| Validation | FluentValidation | ✅ Confirmed | Complex password rules support |

### 3.2 Project-Specific Requirements Analysis

**Additional Requirements**:
- **Key Management**: On-premises RSA key storage with Data Protection API
- **MFA Provider**: TOTP only (Google Authenticator compatible)
- **Email Delivery**: MailKit with on-premises SMTP server
- **Background Jobs**: IHostedService for session cleanup
- **Rate Limiting**: Built-in ASP.NET Core rate limiting middleware
- **Health Checks**: Built-in health check endpoints
- **API Documentation**: Swagger/OpenAPI

### 3.3 Deviation Justifications

**No deviations from default stack** - All standard choices meet project requirements.

### 3.4 Cloud Services and Infrastructure

**On-Premises Deployment**:
- Windows Server 2022 or later
- IIS 10+ or Kestrel behind reverse proxy
- SQL Server 2019+ (Enterprise for HA)
- Local certificate management

**Optional Cloud Services** (Hybrid Mode):
- Azure Key Vault for key management
- Azure Service Bus for event distribution
- Application Insights for monitoring

### 3.5 Development Tools and Environment Setup

| Tool | Purpose | Version |
|------|---------|---------|
| Visual Studio 2022 | Primary IDE | 17.8+ |
| SQL Server Developer | Local database | 2019+ |
| Docker Desktop | Container development | Latest |
| Postman | API testing | Latest |
| Git | Version control | 2.40+ |

### 3.6 Third-Party Libraries and Dependencies

| Library | Purpose | Version | License |
|---------|---------|---------|---------|
| BCrypt.Net-Next | Password hashing | 4.0.3 | MIT |
| System.IdentityModel.Tokens.Jwt | JWT handling | 7.0+ | MIT |
| MailKit | Email delivery via SMTP | 4.0+ | MIT |
| MediatR | CQRS pattern | 12.0+ | MIT |
| FluentValidation | Input validation | 11.0+ | Apache 2.0 |
| Serilog.AspNetCore | Logging provider | 8.0+ | Apache 2.0 |

**Built-in .NET Libraries Used**:
- Microsoft.AspNetCore.Authentication.JwtBearer (JWT authentication)
- Microsoft.AspNetCore.DataProtection (MFA secret encryption)
- Microsoft.Extensions.Caching.Memory (Session caching)
- Microsoft.Extensions.Hosting (Background services)
- Microsoft.AspNetCore.RateLimiting (Rate limiting)
- Microsoft.Extensions.Diagnostics.HealthChecks (Health monitoring)
- Microsoft.Extensions.Options (Configuration)

### 3.7 Technology Decision Documentation

**Decision Record: JWT Implementation**
- **Decision**: Use RS256 asymmetric signing
- **Rationale**: Enables key rotation without token invalidation
- **Alternative**: HS256 symmetric (rejected - single key vulnerability)
- **PRD Alignment**: ISO 27001 requirement for key management

**Decision Record: Database Connection String Management**
- **Decision**: Windows Authentication with service account
- **Implementation**: 
  - Connection string in appsettings.Production.json
  - Format: `Server=SQLSERVER01;Database=TransLution;Integrated Security=true;`
  - App Pool runs under domain service account
  - Service account has db_datareader, db_datawriter, db_ddladmin roles
- **Alternative for SQL Auth**: 
  - Use Data Protection API to encrypt connection string section
  - Store encrypted string in appsettings.Production.json
- **Rationale**: Simple, secure, no external dependencies

## 4. Data Architecture

### 4.1 Data Model Design (ERD/Schema)

**Core Entities**:

```
Users
├── UserId (uniqueidentifier, PK)
├── Username (nvarchar(255), unique, indexed)
├── Email (nvarchar(255))
├── PasswordHash (nvarchar(255))
├── Roles (nvarchar(500))
├── MfaEnabled (bit)
├── MfaSecret (varbinary(max), encrypted)
├── IsLocked (bit)
├── LockoutEnd (datetime2)
├── FailedLoginAttempts (int)
├── LastLoginAttempt (datetime2)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2)

ActiveSessions
├── SessionId (uniqueidentifier, PK)
├── UserId (uniqueidentifier, FK, indexed)
├── RefreshTokenJti (nvarchar(255), unique, indexed)
├── DeviceInfo (nvarchar(500))
├── IpAddress (nvarchar(45))
├── CreatedAt (datetime2)
├── ExpiresAt (datetime2, indexed)
└── LastActivity (datetime2)

AuditLog
├── LogId (bigint, PK, identity)
├── UserId (uniqueidentifier, indexed)
├── Username (nvarchar(255))
├── EventType (nvarchar(100), indexed)
├── IpAddress (nvarchar(45))
├── UserAgent (nvarchar(1000))
├── Timestamp (datetime2, indexed)
└── Details (nvarchar(max))

DeviceCertificates
├── CertificateId (uniqueidentifier, PK)
├── DeviceSerial (nvarchar(100), unique)
├── Certificate (varbinary(max))
├── IssuedAt (datetime2)
├── ExpiresAt (datetime2)
└── IsRevoked (bit)
```

### 4.2 Database Design Patterns

**Patterns Applied**:
- **Soft Delete**: Not used - explicit session deletion for security
- **Audit Columns**: CreatedAt/UpdatedAt on all entities
- **Encryption**: Data Protection API for MFA secrets
- **Indexing Strategy**: Username, session tokens, audit timestamps
- **Partitioning**: AuditLog by month (only if exceeding 1M records)

### 4.3 Data Storage Strategy

**Storage Tiers**:
| Data Type | Storage | Retention | Backup |
|-----------|---------|-----------|---------|
| User Data | Primary DB | Permanent | Daily |
| Active Sessions | Primary DB + Memory Cache | 60 minutes | Real-time |
| Audit Logs | Primary DB | 90 days hot, 2 years archive | Daily |
| Device Certificates | Primary DB | Until revoked | Daily |

### 4.4 Data Migration and Versioning

**Migration Strategy**:
1. EF Core migrations for schema changes
2. Backward compatibility for 1 version
3. Blue-green deployment support
4. Zero-downtime migration scripts

**Version Control**:
- Database version in configuration table
- Migration history tracked by EF Core
- Rollback scripts for each migration

### 4.5 Data Backup and Recovery

**Developer Requirements for Backup Support**:
- **Database Design**: Include timestamp columns for incremental backup support
- **Session Table Design**: ActiveSessions can be truncated without data loss
- **Audit Trail**: AuditLog must support date-range queries for archival
- **No Special Code**: Standard SQL Server backup/restore compatibility required

**Recovery Considerations**:
- Application must handle empty ActiveSessions table gracefully
- Failed login counters should be resettable via admin API
- No in-memory state that would be lost during restore

### 4.6 Data Governance and Compliance

**ISO 27001 Requirements**:
| Requirement | Implementation |
|-------------|----------------|
| Data Classification | User data = Confidential, Logs = Internal |
| Access Control | Role-based with least privilege |
| Encryption at Rest | TDE for database, column encryption for secrets |
| Encryption in Transit | TLS 1.2+ mandatory |
| Data Retention | 90-day active logs, 2-year archive |
| Right to Erasure | User anonymization procedures |

## 5. API Design and Integration

### 5.1 API Architecture (REST)

**Design Principles**:
- RESTful resource-based design
- Stateless with JWT bearer tokens
- JSON request/response format
- Consistent error response structure

### 5.2 API Specification and Documentation

**Authentication Endpoints**:

| Endpoint | Method | Purpose | Auth Required |
|----------|--------|---------|---------------|
| /api/auth/login | POST | User authentication | No |
| /api/auth/refresh | POST | Token refresh | Refresh token |
| /api/auth/logout | POST | Session termination | Access token |
| /api/auth/logout-all | POST | Terminate all sessions | Access token |
| /api/auth/verify | GET | Token validation | Access token |
| /api/auth/sessions | GET | List active sessions | Access token |
| /api/auth/mfa/setup | POST | Configure MFA | Access token |
| /api/auth/mfa/verify | POST | Verify MFA code | Access token |

### 5.3 Request/Response Schemas

**Schema Definitions**: See PRD Section 3.3 for detailed request/response schemas.

**OpenAPI Implementation**:
```
Configuration:
- Swagger/Swashbuckle.AspNetCore for auto-generation
- Generate from controller attributes and XML comments
- JWT Bearer authentication scheme defined
- API versioning: URL path strategy (/api/v1/)

Security Scheme Definition:
- Type: HTTP
- Scheme: bearer
- Bearer Format: JWT
- In: header (Authorization: Bearer {token})

Documentation Features:
- Example values for all endpoints
- Error response examples (400, 401, 429, 500)
- Schema validation rules visible
- Try-it-out functionality enabled for testing
```

**Response Standards**:
- All responses wrapped in consistent envelope
- Correlation ID in headers (X-Correlation-Id)
- API version in headers (X-API-Version)
- Rate limit headers on all responses

### 5.4 Authentication and Authorization

**Token Structure Requirements**:
- Access Token: 15-minute expiry, user claims
- Refresh Token: 60-minute expiry, minimal claims
- Both signed with RS256 algorithm
- Key rotation every 90 days

**Authorization Rules**:
| Resource | Required Role | Additional Check |
|----------|--------------|------------------|
| /api/auth/* | None (public) | Rate limiting |
| /api/flows/* | User or Admin | Valid session |
| /api/admin/* | Admin | MFA required |

### 5.5 External API Integrations

| Service | Purpose | Integration Type | Implementation |
|---------|---------|------------------|----------------|
| SMTP Server | Email delivery | On-premises SMTP | MailKit library |
| TOTP | Authenticator apps | RFC 6238 | Built-in implementation |

**Email Configuration**:
- Use existing on-premises SMTP server
- MailKit for modern SMTP handling
- Configuration via appsettings.json
- No external email service dependencies

### 5.6 Rate Limiting and Throttling

**Built-in ASP.NET Core Rate Limiting Configuration**:
| Endpoint | Limit | Window | Policy Type |
|----------|-------|--------|-------------|
| /api/auth/login | 5 | 15 min | Fixed window per username |
| /api/auth/refresh | 10 | 1 min | Sliding window per IP |
| /api/auth/mfa/verify | 5 | 5 min | Fixed window per user |
| All other | 100 | 1 min | Sliding window per IP |

**Implementation Details**:
```
Rate Limiter Configuration:
- Use AddRateLimiter in Program.cs
- Memory store for 500 users (no Redis needed)
- Custom key extraction for username-based limiting
- Fallback to IP when username unavailable

Rate Limit Headers:
- X-RateLimit-Limit: Request limit per window
- X-RateLimit-Remaining: Requests remaining
- X-RateLimit-Reset: UTC timestamp of window reset
- Retry-After: Seconds until next request allowed (429 only)

Algorithm Choice:
- Fixed window: For security-critical endpoints (login)
- Sliding window: For general API protection
- Token bucket: Not needed at this scale
```

**Security Middleware Pipeline**:
1. Rate Limiting (first line of defense)
2. CORS (configured origins only)
3. Security Headers
4. Authentication
5. Authorization
6. Request Validation

### 5.7 API Security Implementation

**Security Headers Configuration**:
```
Required Headers:
- Strict-Transport-Security: max-age=31536000; includeSubDomains
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Content-Security-Policy: default-src 'self'
- Referrer-Policy: strict-origin-when-cross-origin

Implementation:
- Custom middleware for header injection
- Different CSP for Swagger UI endpoint
- Remove Server header for security
```

**CORS Configuration**:
```
Allowed Origins:
- Production web app URL
- Mobile app origins (if applicable)
- No wildcard origins allowed

Allowed Methods: GET, POST, PUT, DELETE
Allowed Headers: Authorization, Content-Type, X-Correlation-Id
Exposed Headers: X-RateLimit-*, X-Correlation-Id
Max Age: 86400 (24 hours)
Credentials: true (for cookies if used)
```

**Request Validation Pipeline**:
1. Model state validation (automatic via attributes)
2. FluentValidation for complex rules
3. Input sanitization middleware
4. SQL injection prevention via EF Core parameterization
5. Maximum request size limits (2MB default)

**API Versioning Strategy**:
```
Version Strategy: URL Path (/api/v1/)
Supported Versions: v1 (current)
Deprecation Policy: 6-month notice
Version Header: X-API-Version (informational)
Default Version: Latest when not specified
```

## 6. Feature Implementation

### 6.1 Core Feature Technical Breakdown

**Mobile PIN Authentication**:
- 4-digit PIN stored as bcrypt hash
- Device certificate validation using Data Protection API
- Session tied to device + user combination
- Auto-logout on device change

**Desktop Password Authentication**:
- Username/password with bcrypt validation
- Optional TOTP MFA challenge
- Remember me via extended refresh token
- Session persistence in sessionStorage

**MFA Implementation (TOTP Only)**:
- Google Authenticator compatible
- RFC 6238 compliant implementation
- QR code generation for easy setup
- Backup codes stored with Data Protection API encryption

**Mobile Device Certificate Generation**:
- Admin initiates device registration in web portal
- System generates unique certificate using Data Protection API
- Certificate includes: DeviceId, IssuedDate, ExpiryDate
- Store certificate hash in DeviceCertificates table
- Device stores full certificate locally
- Validation: Device presents cert → hash comparison
- Revocation: Set IsRevoked flag in database
- No PKI infrastructure required

### 6.2 Business Logic Implementation

**Password Complexity Rules** (Admin Configurable):
| Rule | Default | Range |
|------|---------|-------|
| Minimum Length | 8 | 6-20 |
| Uppercase Required | Yes | Yes/No |
| Lowercase Required | Yes | Yes/No |
| Number Required | Yes | Yes/No |
| Special Char Required | No | Yes/No |

**Account Lockout Logic**:
1. Track failed attempts with timestamp
2. Lock after 5 failures in 15 minutes
3. Auto-unlock after 30 minutes
4. Admin override capability
5. Audit all lockout events

### 6.3 User Interface Technical Requirements

**Mobile Requirements**:
- Minimum touch target: 48x48px
- Font size: 16px minimum
- Contrast ratio: 7:1 minimum
- Response time: <100ms for UI feedback

**Desktop Requirements**:
- Keyboard navigation support
- ARIA labels for accessibility
- Progressive enhancement
- Browser support: Chrome 90+, Edge 90+, Firefox 88+

### 6.4 Algorithm Specifications

**Token Generation**:
1. Generate cryptographically secure JTI
2. Build claims from user + session data
3. Sign with current RSA private key
4. Include key ID in header
5. Return base64url encoded token

**Session Cleanup Service**:
1. Implemented as IHostedService
2. Configurable interval via appsettings.json
3. Query expired sessions (ExpiresAt < now)
4. Delete in batches (batch size configurable)
5. Log cleanup statistics
6. Include enable/disable flag in configuration

**Implementation Pattern**:
```
Configuration Structure:
- SessionCleanup:Enabled (bool)
- SessionCleanup:IntervalMinutes (int)
- SessionCleanup:BatchSize (int)

Service Requirements:
- BackgroundService base class
- Configurable timer interval
- Direct DbContext usage
- Structured logging of results
- Graceful shutdown on application stop
```

### 6.5 Real-time and Asynchronous Processing

**Asynchronous Operations**:
| Operation | Implementation | Configuration Support |
|-----------|---------------|----------------------|
| Audit logging | Fire-and-forget via ILogger | Log level configurable |
| Email delivery | Async with configurable retry | MaxRetries, DelayMs in config |
| Session cleanup | Configurable IHostedService | Interval, BatchSize in config |
| Cache expiration | TTL-based automatic expiry | CacheDuration in config |

**Background Services Requirements**:
- Must support enable/disable via configuration
- Interval/frequency must be configurable
- All timings in configuration, not hardcoded
- Graceful shutdown on application stop
- Health check integration for service status

### 6.6 Feature Flag and Configuration Management

**Configuration Categories**:
| Category | Storage | Update Method |
|----------|---------|---------------|
| Security settings | appsettings.json | IOptionsMonitor |
| Rate limits | appsettings.json | Deployment |
| Feature flags | Database | Admin UI |
| MFA providers | appsettings.json | Deployment |

## 7. Non-Functional Implementation

### 7.1 Performance Architecture

**Performance Requirements**:
| Feature | Requirement | Implementation |
|---------|------------|----------------|
| Login response | <500ms target | Async operations, indexed queries |
| Token validation | <50ms target | In-memory validation |
| Concurrent users | Support 500 | Stateless design |
| Cache implementation | IMemoryCache | Configurable size limits |

**Caching Implementation**:
- All cache durations configurable via appsettings
- Memory cache with configurable size limit
- TTL-based expiration (no manual invalidation)
- Cache keys must include tenant/environment prefix

**Database Requirements**:
- Indexes on: Username, SessionTokens, Timestamp fields
- All queries use async/await
- Connection pooling handled by .NET defaults
- Query timeout configurable in connection string

### 7.2 Security Implementation

**Encryption Standards**:
| Data Type | Method | Implementation |
|-----------|--------|----------------|
| Passwords | BCrypt (12 rounds) | BCrypt.Net-Next |
| MFA secrets | Data Protection API | ASP.NET Core built-in |
| JWT signing | RSA 2048-bit | .NET cryptography |
| Data in transit | TLS 1.2+ | IIS/Kestrel configuration |
| Sensitive config | Data Protection API | Configuration encryption |

**Data Protection API Configuration**:
- Purpose-based key derivation
- Automatic key rotation support
- Keys stored in file system (on-premises)
- Backup key storage location configured

**Vulnerability Management**:
- OWASP Top 10 compliance
- Regular dependency updates via NuGet
- Security headers via middleware
- Input validation on all endpoints
- Anti-forgery tokens where applicable

### 7.3 Scalability Solutions

**Current Scale (500 Users)**:
- Single server deployment capable
- IMemoryCache sufficient for caching
- Direct database queries performant
- No distributed systems required

**Growth Triggers for Scaling**:
| Users | Trigger | Action Required |
|-------|---------|-----------------|
| 1,000 | Response time >750ms | Add second server + load balancer |
| 2,000 | Cache memory >500MB | Consider Redis cache |
| 5,000 | DB connections >80 | Add read replica |
| 10,000 | Complex ops slow | Consider service extraction |

**Scaling Preparation**:
- Stateless authentication ready
- Database-backed sessions support distribution
- No server affinity required
- Configuration supports multi-instance

### 7.4 Reliability and Resilience

**Retry Policy Implementation**:
| Operation | Requirement | Configuration |
|-----------|-------------|---------------|
| Email sending | Retry transient failures | MaxRetries, DelayMs in config |
| Database operations | Retry on transient errors | Use EF Core retry policy |

**Error Handling Requirements**:
- Global exception handler middleware required
- Structured error responses (see API spec)
- Correlation ID included in all errors
- User-friendly messages (no stack traces)
- All errors logged with full context

**Health Check Implementation**:
- Endpoint: /health
- Check database connectivity
- Return simple UP/DOWN status
- Optional detailed checks at /health/detailed
- Must complete within 5 seconds

## 8. Development Practices

### 8.1 Code Architecture and Patterns

**Applied Patterns**:
- Vertical Slice Architecture
- CQRS with MediatR
- Repository pattern (thin wrapper)
- Domain-driven design terminology
- Dependency injection throughout

### 8.2 Development Workflow

1. Feature branch from develop
2. Implement vertical slice
3. Unit + integration tests
4. Code review (2 approvers)
5. Merge to develop
6. Automated deployment to test

### 8.3 Code Quality Standards

**Quality Gates**:
- Code coverage: 80% minimum
- Cyclomatic complexity: <10
- No critical SonarQube issues
- All public APIs documented
- Performance tests pass

### 8.4 Documentation Requirements

**Required Documentation**:
- XML comments on public APIs
- README in each feature folder
- Architecture decision records
- API documentation (Swagger)
- Deployment runbooks

### 8.5 Peer Review Process

**Review Checklist**:
- [ ] Follows vertical slice pattern
- [ ] Includes appropriate tests
- [ ] Handles errors gracefully
- [ ] Meets performance targets
- [ ] Security best practices
- [ ] No unnecessary complexity

### 8.6 Technical Debt Management

**Debt Categories**:
| Priority | Examples | Resolution |
|----------|----------|------------|
| High | Security vulnerabilities | Next sprint |
| Medium | Missing tests | Within quarter |
| Low | Code cleanup | As capacity allows |

## 9. Testing Strategy

### 9.1 Testing Philosophy and Approach

**Testing Principles**:
- Test behavior, not implementation
- Focus on business value
- Automate regression tests
- Manual testing for UX
- Performance testing for SLAs

### 9.2 Unit Testing Framework

**Framework**: xUnit + FluentAssertions + NSubstitute
**Coverage Target**: 80% for business logic
**Key Test Areas**:
- Password validation logic
- Token generation/validation
- Rate limiting logic
- Business rule enforcement

### 9.3 Integration Testing Strategy

**Test Scenarios**:
- Full authentication flow
- Session management lifecycle
- MFA setup and verification
- Database transaction integrity
- External API integration

**Test Data**:
- Separate test database
- Seeded test users
- Mock external services
- Realistic data volumes

### 9.4 End-to-End Testing

**Critical User Journeys**:
1. Mobile device first login
2. Desktop login with MFA
3. Session timeout handling
4. Password reset flow
5. Multi-device scenarios

### 9.5 Performance and Load Testing

**Load Test Scenarios**:
| Scenario | Users | Duration | Success Criteria |
|----------|-------|----------|------------------|
| Normal load | 100 | 30 min | <500ms response |
| Peak load | 500 | 15 min | <1s response |
| Spike test | 1000 | 5 min | No errors |
| Endurance | 200 | 4 hours | Stable memory |

### 9.6 Security Testing

**Security Test Areas**:
- Penetration testing (quarterly)
- OWASP dependency check (build)
- SQL injection testing
- JWT manipulation attempts
- Brute force protection

### 9.7 Test Automation Pipeline

**Pipeline Stages**:
1. Unit tests (every commit)
2. Integration tests (every PR)
3. Security scan (every PR)
4. Performance tests (nightly)
5. E2E tests (pre-release)

## 10. Deployment and Operations

### 10.1 Deployment Architecture

**Production Topology (500 Users)**:
```
Internet → Firewall → Single Server
                           ↓
                    [IIS/Kestrel]
                    [Application]
                           ↓
                    [SQL Server]
                    (Same or separate server)
```

**Future Scale (1000+ Users)**:
```
Internet → Firewall → Load Balancer
                           ↓
                    [Web Servers x2]
                           ↓
                    [SQL Server]
                    (Always On AG)
```

### 10.2 CI/CD Pipeline Design

**Pipeline Stages**:
1. **Build**: Compile, restore packages
2. **Test**: Unit + integration tests
3. **Analyze**: Code quality, security scan
4. **Package**: Create deployment artifacts
5. **Deploy Dev**: Automated deployment
6. **Deploy Test**: After approval
7. **Deploy Prod**: Manual trigger

### 10.3 Environment Management

| Environment | Purpose | Data | Access |
|-------------|---------|------|--------|
| Development | Feature development | Synthetic | Developers |
| Test | Integration testing | Anonymized | Dev + QA |
| Staging | Pre-production | Production copy | Limited |
| Production | Live system | Real data | Operations |

### 10.4 Infrastructure as Code

**IaC Components**:
- Server provisioning scripts
- IIS configuration
- SSL certificate deployment
- Firewall rules
- SQL Server setup

**Not Cloud-Native**: Focus on scriptable on-premises deployment

### 10.5 Container Strategy

**Container Usage**:
- Development environment only
- Docker Compose for local stack
- Simplifies developer onboarding
- Not for production (on-premises requirement)

**Local Development Stack**:
- ASP.NET Core app container
- SQL Server Developer container
- MailDev for SMTP testing
- Single docker-compose.yml

### 10.6 Deployment Requirements

**Application Deployment Support**:
1. **Configuration Management**:
   - Environment-specific appsettings.{Environment}.json
   - Support for configuration transforms
   - Sensitive settings use Data Protection API
   
2. **Maintenance Mode**:
   - Configurable maintenance flag
   - Return 503 with maintenance message
   - Allow admin override access
   
3. **Database Migrations**:
   - EF Core migrations support
   - Forward-only migrations (no down scripts)
   - Migration runner on startup (configurable)
   
4. **Version Information**:
   - Assembly version in responses headers
   - Version endpoint: /api/version
   - Build number from CI/CD pipeline

**Session Handling**:
- Application must handle session table truncation
- No in-memory session state
- Stateless authentication design

## 11. Monitoring and Observability

### 11.1 Application Monitoring

**Key Metrics**:
| Metric | Implementation | Threshold |
|--------|----------------|-----------|
| Login success rate | Counter metric | Alert if <90% |
| Response time | Histogram | Alert if >1s |
| Active sessions | Gauge | Information only |
| Error rate | Counter | Alert if >5% |

### 11.2 Infrastructure Monitoring

**Monitored Resources**:
- CPU utilization
- Memory usage
- Disk I/O
- Network latency
- Database connections

### 11.3 Logging Strategy

**Structured Logging with Serilog**:
| Level | Usage | Examples |
|-------|-------|----------|
| Error | System errors | Database connection failed |
| Warning | Recoverable issues | Rate limit exceeded |
| Information | Business events | User logged in |
| Debug | Troubleshooting | Token validation details |

**Correlation ID Implementation**:
```
Generation and Propagation:
- Generated at entry point (GUID format)
- Stored in AsyncLocal<string> for thread safety
- Automatically included in all log entries
- Passed to all downstream operations
- Returned in response header (X-Correlation-Id)

Correlation Scenarios:
- Login flow: Auth → Validation → Token Generation → Session → Audit
- MFA flow: Challenge → Send Email → Verify → Update Session
- Error flows: Track error through all retry attempts

Implementation Pattern:
- Middleware to generate/extract correlation ID
- Serilog enricher to add to all logs
- Pass in MediatR command/query base class
- Include in all external API calls
```

**Structured Logging Fields**:
```
Standard Fields:
- Timestamp: UTC timestamp
- Level: Log level
- Message: Log message
- Exception: Exception details if applicable
- CorrelationId: Request correlation ID

Business Context:
- UserId: Authenticated user ID
- SessionId: Current session ID
- Username: For audit purposes
- EventType: Business event type

Technical Context:
- MachineName: Server name
- Environment: Dev/Test/Prod
- Version: Application version
- RequestPath: API endpoint
- Duration: Operation duration
- ResponseCode: HTTP status
```

**Log Storage and Querying**:
- Local files with daily rotation
- 30-day retention on server
- Archive to network share
- Structured format enables correlation queries
- Example query: "Show all logs for CorrelationId X"

**Correlation ID Usage Examples**:
| Operation | Log Messages with Same CorrelationId |
|-----------|--------------------------------------|
| Successful Login | Request received → User validated → Token generated → Session created → Response sent |
| Failed Login | Request received → Validation failed → Lockout checked → Audit logged → Error returned |
| MFA Flow | MFA required → Email sent → Code validated → Session updated → Success logged |

### 11.4 Alerting and Incident Response

**Application Alerting Requirements**:
- Log events at appropriate levels (Error, Warning, Info)
- Include context in all log entries (user, operation, duration)
- Emit metrics for monitoring integration
- Structure logs for easy querying

**Key Events to Log**:
| Event | Log Level | Required Context |
|-------|-----------|------------------|
| Login failure | Warning | Username, IP, reason |
| Account locked | Warning | Username, timestamp |
| System error | Error | Full exception, correlation ID |
| Slow operation | Warning | Operation name, duration |
| Configuration change | Info | Changed setting, old/new value |

### 11.5 Performance Metrics and SLAs

**Application Metrics to Expose**:
- Request duration histograms
- Active session count gauge
- Login success/failure counters
- Cache hit/miss ratios
- Database query durations

**Required Metric Endpoints**:
- **/metrics** - Prometheus-compatible metrics
- **/health** - Basic health status
- **/info** - Application version and environment

**Performance Requirements**:
- All metrics calculated in-memory
- Metrics endpoint must respond within 100ms
- No database queries for metrics collection

### 11.6 Business Metrics Tracking

**Tracked Metrics**:
- Daily active users
- Login success rate by device type
- MFA adoption rate
- Average session duration
- Failed login patterns

## 12. Security Architecture

### 12.1 Security Framework

**Security Layers**:
1. Network security (firewall, IDS)
2. Application security (auth, encryption)
3. Data security (encryption, access control)
4. Operational security (monitoring, incident response)

### 12.2 Identity and Access Management

**Access Control Model**:
- Role-based access control (RBAC)
- Least privilege principle
- Regular access reviews
- Automated de-provisioning

**Roles Defined**:
| Role | Permissions | MFA Required |
|------|-------------|--------------|
| Mobile User | Basic app access | No |
| Flow Creator | Create/edit flows | Optional |
| Administrator | Full system access | Yes |

### 12.3 Data Protection and Privacy

**Data Protection Implementation**:
| Data Type | Protection Method | Implementation |
|-----------|------------------|----------------|
| Passwords | One-way hash | BCrypt.Net-Next |
| MFA secrets | Reversible encryption | Data Protection API |
| Session tokens | Signed + temporary | JWT with expiry |
| Email/logs | Pseudonymization | Hash PII in logs |
| Backup data | Encrypted backup | SQL TDE + backup encryption |

**Data Protection API Usage**:
- Protect MFA secrets before storage
- Protect sensitive configuration values
- Key storage in %PROGRAMDATA%\Keys
- Automatic key rotation every 90 days

### 12.4 Network Security

**Application Security Requirements**:
- Enforce HTTPS-only (redirect HTTP to HTTPS)
- Support configurable TLS versions (min 1.2)
- Certificate validation for outbound connections
- Support certificate pinning for mobile apps

**Certificate Handling**:
- Read certificates from Windows Certificate Store
- Support certificate thumbprint in configuration
- Mobile apps must support certificate pinning
- Log certificate validation failures

**Security Headers Implementation**:
- Apply security headers via middleware
- Headers must be configurable
- Different policies for API vs Swagger UI
- See Section 5.7 for required headers

### 12.5 Application Security

**Security Controls**:
- Input validation (all inputs)
- Output encoding (XSS prevention)
- Parameterized queries (SQL injection)
- Security headers (CSP, HSTS, etc.)
- Regular security updates

### 12.6 Compliance Requirements

**ISO 27001 Compliance**:
| Control | Implementation |
|---------|----------------|
| Access Control | JWT + RBAC |
| Cryptography | Industry standard algorithms |
| Logging | Comprehensive audit trail |
| Incident Management | Defined response procedures |
| Business Continuity | Backup and recovery plans |

## 13. Scalability and Future Planning

### 13.1 Growth Projections and Scaling Plans

**Scalability Design Requirements**:
- Stateless authentication (no server affinity)
- Database-backed sessions (not in-memory)
- Configurable cache sizes and limits
- No hardcoded limits in code
- Support for multiple instance deployment

**Configuration for Scale**:
```
Required Configuration Options:
- Cache:MaxSizeMB
- RateLimit:PerUserLimit
- Database:CommandTimeout
- Sessions:CleanupBatchSize
- Threading:MaxConcurrentRequests
```

**Code Design for Future Scale**:
- Use interfaces for future service extraction
- Avoid static/singleton state
- Design for horizontal scaling
- Use async/await throughout
- Implement circuit breaker pattern for external calls

### 13.2 Technical Debt Roadmap

**Future Feature Preparation**:
- Design authentication service with clear interfaces
- Keep authentication logic separate from business logic
- Use feature flags for new capabilities
- Maintain backward compatibility for API changes

**Extension Points to Build**:
- Authentication provider interface (for future biometric/WebAuthn)
- Pluggable MFA providers
- Configurable password policies
- Custom claim providers
- Event hooks for authentication events

### 13.3 Architecture Evolution Strategy

**Evolution Triggers**:
- Performance degradation
- Scaling limitations
- New security requirements
- Business growth

**Evolution Path**:
1. Monolith (current)
2. Modular monolith
3. Service extraction
4. Microservices (if needed)

### 13.4 Technology Upgrade Paths

**Planned Upgrades**:
| Component | Current | Target | Timeline |
|-----------|---------|--------|----------|
| .NET | 8.0 | 9.0 | As needed |
| SQL Server | 2019 | 2022 | As needed |
| Auth Standard | JWT | JWT + WebAuthn | Future |

### 13.5 Microservices Migration (if applicable)

**Migration Criteria** (must meet all):
- >10,000 active users
- Performance bottlenecks identified
- Team capacity for distributed systems
- Business case for complexity

**Target Architecture** (Future State):
- Auth service (separate deployment)
- User service (separate deployment)
- Session service (with Redis)
- Event bus for communication

## 14. Appendices

### 14.1 Sequence Diagrams

**Login Sequence**:
1. Client sends credentials
2. Validate username/password
3. Check account status
4. Generate tokens
5. Create session
6. Log audit event
7. Return tokens

**Token Refresh Sequence**:
1. Client sends refresh token
2. Validate token signature
3. Check session validity
4. Generate new access token
5. Update session activity
6. Return new token

### 14.2 Database Schemas

**Index Strategy**:
- Users.Username (unique, nonclustered)
- ActiveSessions.RefreshTokenJti (unique, nonclustered)
- ActiveSessions.ExpiresAt (nonclustered)
- AuditLog.Timestamp (clustered)
- AuditLog.UserId (nonclustered)

### 14.3 API Documentation

**Standard Response Headers**:
- X-Request-ID: Correlation identifier
- X-RateLimit-Limit: Request limit
- X-RateLimit-Remaining: Requests remaining
- X-RateLimit-Reset: Reset timestamp

### 14.4 Technical Decision Records

**ADR-001: JWT over Session Cookies**
- **Context**: Need stateless authentication
- **Decision**: Use JWT tokens
- **Consequences**: More complex but scalable

**ADR-002: Vertical Slices Architecture**
- **Context**: Maintain feature cohesion
- **Decision**: Organize by feature not layer
- **Consequences**: Easier to modify features

### 14.5 Performance Benchmarks

**Required Performance Characteristics**:
| Operation | Target | How to Achieve |
|-----------|--------|----------------|
| Login | <500ms | Async operations, indexed queries |
| Token Validation | <50ms | In-memory validation, no DB calls |
| Session Query | <100ms | Indexed on session token |
| Health Check | <100ms | Simple DB connectivity test |

**Load Testing Requirements**:
- Application must support load testing
- Test endpoints must handle 500 concurrent users
- Include sample load test scripts
- Document performance testing approach

---

**Document Version**: 1.0  
**Last Updated**: July 2025  
**Next Review**: October 2025  
**Owner**: TransLution Engineering Team

This specification provides the technical blueprint for implementing JWT authentication that meets ISO 27001 compliance while maintaining the pragmatic approach needed for mid-sized manufacturing environments.