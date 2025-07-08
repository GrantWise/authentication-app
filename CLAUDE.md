# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 🚨 MANDATORY: Read AI Coding Instructions First
**BEFORE writing ANY code**, read and follow `docs/ai-coding-instructions.md`. These are non-negotiable directives.

## Repository Structure

This is a full-stack authentication system with:
- `frontend/` - Next.js 15 application  
- `backend/` - ASP.NET Core 8 Web API with feature-based vertical slice architecture
- `docs/` - Project documentation including PRD and AI coding instructions

## ⚠️ CRITICAL: Architecture Rules (NO EXCEPTIONS)
- **ALWAYS** group by features, **NEVER** by technical layers
- **ALWAYS** put controller, service, models, tests for same feature in same folder
- **FORBIDDEN:** Splitting related functionality across technical layers
- **REQUIRED:** Business domain naming that expresses intent clearly

## Common Commands

**Frontend Development (run from `frontend/` directory):**
- `npm run dev` - Start development server with Turbopack (opens http://localhost:3000)
- `npm run build` - Build production application
- `npm run start` - Start production server
- `npm run lint` - Run ESLint to check code quality
- `npm install` - Install dependencies

**Backend Development (run from `backend/Authentication.Api/` directory):**
- `dotnet run` - Start the API server (opens https://localhost:7157)
- `dotnet build` - Build the C# application
- `dotnet restore` - Restore NuGet packages
- `dotnet ef database update` - Apply database migrations

## Architecture Overview

### Frontend: Next.js 15 Application
This is a Next.js 15 application using the App Router architecture with TypeScript and Tailwind CSS.

### Backend: ASP.NET Core 8 with Vertical Slice Architecture
JWT-based authentication API following vertical slice architecture patterns.

**Frontend Technologies (FIXED - NO SUBSTITUTIONS):**
- Next.js 15 with App Router  
- TypeScript with strict mode (REQUIRED)
- Tailwind CSS v4 for styling
- shadcn/ui components (ONLY ui library allowed)
- TanStack Query for server state (REQUIRED)
- Zustand for global UI state (REQUIRED) 
- React Hook Form + Zod for forms (REQUIRED)
- ESLint with Next.js rules
- Turbopack for fast development builds

**Backend Technologies (FIXED - NO SUBSTITUTIONS):**
- ASP.NET Core 8.0 Web API (ONLY backend framework)
- Entity Framework Core with SQL Server (ONLY ORM)
- JWT authentication with RS256 signing
- MediatR for CQRS pattern  
- FluentValidation for request validation (REQUIRED)
- Serilog for structured logging (ONLY logging framework)
- BCrypt for password hashing

## Project Structure

**Frontend (`frontend/` directory):**
- `app/` - Next.js App Router pages and layouts
  - `layout.tsx` - Root layout with Geist fonts
  - `page.tsx` - Homepage component
  - `globals.css` - Global styles
- `lib/` - Utility functions
  - `utils.ts` - Contains `cn()` utility for class merging
- `components.json` - shadcn/ui configuration with New York style
- `public/` - Static assets (SVG icons, images)

**Backend (`backend/Authentication.Api/` directory):**
- `Features/` - Vertical slices (Login, Register, etc.)
  - Each feature contains: Request/Response models, Handlers, Controllers, Validators
- `Common/` - Shared infrastructure
  - `Entities/` - Domain entities (User, Session, AuditLog)
  - `Interfaces/` - Service contracts
  - `Services/` - Shared service implementations
  - `Data/` - Entity Framework DbContext
- `Program.cs` - Application startup and configuration

## Frontend Component System
- Uses shadcn/ui component library with "new-york" style
- Configured with Lucide icons
- Path aliases: `@/components`, `@/lib/utils`, `@/components/ui`
- CSS variables enabled for theming

## Backend Architecture
- **Vertical Slice Architecture**: Each feature is a complete vertical slice
- **CQRS Pattern**: Using MediatR for request/response handling
- **JWT Authentication**: RS256 asymmetric signing for security
- **Session Management**: Active session tracking and management
- **Audit Logging**: Comprehensive security event logging

## Development Notes

**Frontend:**
- Uses Turbopack for development builds (faster than Webpack)
- ESLint configured with Next.js TypeScript rules
- Ready for shadcn/ui component additions
- Font optimization handled by Next.js font system

**Backend:**
- Database is created automatically on application start
- CORS configured for `http://localhost:3000` (frontend)
- Logs written to both console and file (`logs/authentication-.log`)
- Account lockout: 30-minute lockout after 5 failed login attempts

## 🔒 MANDATORY Security Requirements
- **JWT Authentication**: ALWAYS protect endpoints with [Authorize] by default
- **Input Validation**: ALWAYS validate all input on backend with FluentValidation  
- **Frontend Validation**: ALWAYS validate all forms with Zod
- **Secrets**: FORBIDDEN to commit secrets to source control
- **Logging**: FORBIDDEN to log passwords, tokens, or personal information

## Security Features (ISO 27001 Compliant)
- JWT tokens: 15-minute access tokens, 60-minute refresh tokens with rotation
- RSA 2048-bit key pairs for JWT signing
- BCrypt password hashing with 12+ rounds (configurable)
- Account lockout: 30 minutes after 5 failed attempts
- Rate limiting: 5 attempts per username per 15 minutes
- Comprehensive audit logging with 1-year retention
- TLS 1.3 preferred, TLS 1.2 minimum
- Security headers: HSTS, CSP, X-Frame-Options, etc.
- AES-256 encryption for sensitive data at rest

## 📋 MANDATORY Documentation Requirements
- **XML Documentation**: REQUIRED for ALL public classes, methods, properties
- **API Documentation**: REQUIRED OpenAPI annotations for all endpoints
- **Comments**: ALWAYS comment WHY, never WHAT
- **Self-Documenting**: Code MUST read like business language
- **JSDoc**: REQUIRED for complex frontend business logic