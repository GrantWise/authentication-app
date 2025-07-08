# ServiceBridge AI Coding Agent Instructions

**Purpose:** Mandatory directives for AI agents working on ServiceBridge codebase

---

## MANDATORY DECISION FRAMEWORK

**Before writing ANY code, you MUST verify:**
1. This makes the code easier to understand
2. A human would want to maintain this in six months  
3. A new team member can grasp this quickly
4. This keeps related concepts together

**NEVER choose:** Cleverness over readability, arbitrary rules over logical cohesion, perfect metrics over clear intent.

---

## ARCHITECTURE COMMANDS

### File Organization - NO EXCEPTIONS
- **ALWAYS** group by features, **NEVER** by technical layers
- **ALWAYS** put controller, service, models, tests for same feature in same folder
- **ONLY** use `/Shared` for truly shared utilities
- **FORBIDDEN:** Splitting related functionality across technical layers

### Start Simple - REQUIRED PATTERN
- **ALWAYS** create interfaces for external dependencies (ICacheService, IEmailService)
- **ALWAYS** start with simplest implementation (in-memory, synchronous)
- **NEVER** implement Redis, Hangfire, background jobs until explicitly requested
- **REQUIRED:** Make scaling a config change, not code rewrite

---

## CODE ORGANIZATION RULES

### Class Design - ENFORCE THESE
- **EXACTLY** one primary responsibility per class
- **ALWAYS** show all dependencies in constructor
- **MAXIMUM** 30 lines per method unless single atomic operation
- **REQUIRED:** Use business domain names, not technical names
- **TARGET:** ~200 lines per class, but logical cohesion overrides size

### Naming - STRICT REQUIREMENTS
- **REQUIRED:** Microsoft C# conventions (Async suffix, I prefix, _ prefix)
- **REQUIRED:** Domain-based naming that expresses business intent clearly
- **REQUIRED:** Business terminology, not technical jargon in business logic
- **REQUIRED:** Code reads like business language
- **REQUIRED:** Explicit about what methods do (CreateUser not Process)
- **REQUIRED:** Consistent patterns across entire codebase

---

## DOCUMENTATION - MANDATORY STANDARDS

### Self-Documenting Code - ABSOLUTE REQUIREMENT
- **ALWAYS** use domain-based naming that clearly expresses business intent
- **REQUIRED:** Code must read like business language
- **REQUIRED:** Business terminology consistently throughout
- **FORBIDDEN:** Technical jargon in business logic classes

### XML Documentation - REQUIRED FOR ALL
- **ALWAYS** add XML documentation for all public classes
- **ALWAYS** add XML documentation for all public methods
- **ALWAYS** add XML documentation for all public properties
- **REQUIRED:** Include parameter descriptions and return value descriptions
- **REQUIRED:** Include example usage for complex methods

### API Documentation - MANDATORY
- **ALWAYS** add OpenAPI annotations to all endpoints
- **REQUIRED:** Description, parameter details, response types
- **REQUIRED:** Example request and response payloads
- **REQUIRED:** Error response documentation
- **REQUIRED:** Authentication requirements clearly stated

### Comments - STRICT RULES
- **ALWAYS** comment on the WHY, never the WHAT
- **FORBIDDEN:** Obvious comments that restate code
- **REQUIRED:** Explain business reasoning behind complex logic
- **REQUIRED:** Document any non-obvious business rules or constraints

### Frontend Documentation - REQUIRED
- **ALWAYS** use JSDoc comments for complex business logic
- **ALWAYS** document component interfaces and props
- **REQUIRED:** Document any complex state management patterns
- **REQUIRED:** Explain business purpose of components

---

## ERROR HANDLING - MANDATORY APPROACH

### Custom Exceptions - REQUIRED
- **ALWAYS** create specific exception types for business scenarios
- **ALWAYS** include user-friendly messages in exception constructor
- **ALWAYS** throw exceptions where problems are detected
- **NEVER** use Result wrapper objects or complex error abstractions

### Required Exception Types
- **MUST CREATE:** `NotFoundException` for missing entities
- **MUST CREATE:** `ValidationException` for business rule violations  
- **MUST CREATE:** `BusinessRuleException` for domain logic failures
- **FORBIDDEN:** Generic exceptions for business logic

### Controller Error Handling - STRICT PATTERN
- **ALWAYS** catch specific business exceptions
- **ALWAYS** return appropriate HTTP status codes
- **ALWAYS** log technical details with correlation ID
- **ALWAYS** return user-friendly messages to client
- **FORBIDDEN:** Exposing internal system details to users

---

## DATA ACCESS - NON-NEGOTIABLE RULES

### Entity Framework Usage - REQUIRED
- **ALWAYS** use DbContext directly in service classes initially
- **FORBIDDEN:** Repository pattern until you have specific documented need
- **ALWAYS** make database operations async
- **ALWAYS** use projections (.Select()) for queries
- **REQUIRED:** Check for N+1 problems in every query

### Repository Pattern - ONLY WHEN
- **ONLY** when sharing complex queries across multiple services
- **ONLY** when multiple data sources for same entity exist
- **ONLY** when complex testing scenarios require mocked data access
- **FORBIDDEN:** "Just in case" or "best practice" repositories

---

## FRONTEND COMPONENT RULES - ENFORCE

### Organization - STRICT
- **ALWAYS** place React components with related backend code
- **MAXIMUM** one primary responsibility per component
- **REQUIRED:** TypeScript strict mode, no exceptions
- **REQUIRED:** Explicit types for all props and API payloads

### State Management - NO DEVIATION
- **ALWAYS** use TanStack Query for server state
- **ALWAYS** use Zustand for global UI state
- **ALWAYS** use React Hook Form + Zod for forms
- **FORBIDDEN:** Any other state management libraries

---

## TESTING - MANDATORY APPROACH

### Focus Requirements
- **ALWAYS** test critical business logic first
- **ALWAYS** test happy path before edge cases
- **REQUIRED:** Integration tests for end-to-end flows
- **FORBIDDEN:** Testing just to hit coverage metrics
- **FORBIDDEN:** Unit tests that test implementation details

### Test Organization - STRICT
- **ALWAYS** keep tests in same folder as code
- **REQUIRED:** Test names describe business behavior
- **REQUIRED:** Test outcomes users care about
- **FORBIDDEN:** Testing internal implementation methods

---

## SECURITY - ABSOLUTE REQUIREMENTS

### Input Validation - NO COMPROMISE
- **ALWAYS** validate all input on backend with FluentValidation
- **ALWAYS** validate all forms on frontend with Zod  
- **ALWAYS** treat external input as untrusted
- **FORBIDDEN:** Trusting client-side validation alone

### Authentication - MANDATORY
- **ALWAYS** use JWT for stateless authentication
- **ALWAYS** protect endpoints with [Authorize] by default
- **REQUIRED:** Document any public endpoints with justification
- **REQUIRED:** Implement role-based access control

### Secrets - ABSOLUTE RULES
- **FORBIDDEN:** Committing secrets to source control
- **REQUIRED:** Environment variables for local development
- **REQUIRED:** Secure vaults for production secrets
- **FORBIDDEN:** Logging passwords, tokens, or personal information

---

## TECHNOLOGY STACK - NO SUBSTITUTIONS

### Backend - FIXED CHOICES
- **ONLY** .NET 8 for backend services
- **ONLY** Entity Framework Core for database access
- **ONLY** SQL Server for production, SQLite for local
- **ONLY** Serilog for logging with correlation IDs

### Frontend - FIXED CHOICES
- **ONLY** React with TypeScript
- **ONLY** Tailwind CSS for styling
- **ONLY** shadcn/ui for components
- **REQUIRED:** Follow brand guide for colors

### Technology Changes - FORBIDDEN WITHOUT APPROVAL
- **REQUIRED:** Document specific problem being solved
- **REQUIRED:** Explain why default choice fails
- **REQUIRED:** Get explicit team approval
- **FORBIDDEN:** Introducing new tech without documented justification

---

## SCALING - SPECIFIC TRIGGERS ONLY

### Add Complexity ONLY When These Measured Problems Exist
- **Redis caching:** Database response time >500ms or >1000 queries/minute
- **Background jobs:** Operations taking >30 seconds blocking UI
- **Load balancing:** Single instance CPU >80% or memory >4GB
- **Real-time updates:** Users explicitly request live data updates

### Implementation - REQUIRED PATTERN
- **ALWAYS** start with interface supporting scaling
- **ALWAYS** begin with in-memory/synchronous implementation  
- **ONLY** add infrastructure when triggers are hit
- **REQUIRED:** Scaling through configuration, not code changes

---

## CODE REVIEW - ABSOLUTE FOCUS

### What You MUST Optimize For
- **VERIFY:** Solves actual user problem
- **VERIFY:** Team can understand and maintain
- **VERIFY:** Dependencies are clear and minimal
- **VERIFY:** Error handling fits scenario
- **VERIFY:** Follows existing codebase patterns

### What You MUST NOT Debate
- **FORBIDDEN:** Theoretical performance improvements
- **FORBIDDEN:** Pattern purity without clear benefit
- **FORBIDDEN:** Premature abstractions
- **FORBIDDEN:** Over-engineering for hypothetical needs

---

## ANTI-PATTERNS - ABSOLUTELY FORBIDDEN

### Over-Abstraction - NEVER DO
- **FORBIDDEN:** Interfaces without multiple implementations
- **FORBIDDEN:** Abstracting for testability alone
- **FORBIDDEN:** Building frameworks for single use cases

### Premature Optimization - BANNED
- **FORBIDDEN:** Caching before measuring slow queries
- **FORBIDDEN:** Background jobs before measuring blocking
- **FORBIDDEN:** Microservices before hitting scaling limits

### Breaking Cohesion - PROHIBITED
- **FORBIDDEN:** Separating related code by technical type
- **FORBIDDEN:** Forcing operations through identical layers
- **FORBIDDEN:** Creating artificial boundaries

---

## GIT WORKFLOW - STRICT PROCESS

### Branch Management - NO EXCEPTIONS
- **REQUIRED:** Feature branches from main only
- **REQUIRED:** Main branch always deployable
- **REQUIRED:** Pull requests for all merges
- **REQUIRED:** At least one approval before merge

### Commit Standards - ENFORCE
- **REQUIRED:** Clear commit messages with business impact
- **REQUIRED:** Atomic commits representing complete thoughts
- **FORBIDDEN:** "WIP" or "fix" commit messages in main

---

## AI AGENT DIRECTIVES

### Context Requirements - MANDATORY
- **ALWAYS** request all related feature files before starting
- **ALWAYS** understand business problem before coding
- **REQUIRED:** Ask clarifying questions about unclear requirements
- **VERIFY:** Solution actually solves user problem

### Code Generation Priorities - RANKED ORDER
1. **FIRST:** Working functionality solving the problem
2. **SECOND:** Code team can understand and maintain
3. **THIRD:** Follows established codebase patterns
4. **FOURTH:** Handles errors appropriate to scenario  
5. **FIFTH:** Includes tests for critical paths

### Response Requirements - MANDATORY
- **ALWAYS** explain what problem you're solving
- **ALWAYS** justify architectural decisions against these rules
- **REQUIRED:** Flag any deviations from these directives
- **FORBIDDEN:** Implementing patterns not explicitly allowed here

---

## VIOLATION ESCALATION

**If any directive conflicts with user request:**
1. **REQUIRED:** Explain the conflict clearly
2. **REQUIRED:** Propose alternative that follows directives
3. **REQUIRED:** Get explicit override approval before proceeding
4. **FORBIDDEN:** Silently violating directives