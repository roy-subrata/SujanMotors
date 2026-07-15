# System Architecture

## Purpose

This document defines the overall architecture of the application. It describes how different layers, modules, and components interact while maintaining a scalable, maintainable, and testable codebase.

---

# Architecture Style

The application follows:

- Clean Architecture
- Domain-Driven Design (DDD) where appropriate
- Feature-based modular architecture
- Dependency Injection
- SOLID Principles

(No CQRS/MediatR in this repo — controllers call application services
directly. Do not introduce it without an architecture decision.)

---

# High-Level Architecture

```text
                Angular Frontend
                       │
                  HTTP / HTTPS
                       │
             ASP.NET Core Web API
                       │
          --------------------------
          │                        │
    Application Layer         Infrastructure
          │                        │
          └──────────────┬─────────┘
                         │
                    Domain Layer
                         │
                    Repository Layer
                         │
                     SQL Database
```

---

# Layer Responsibilities

## Presentation Layer

Responsible for

- UI
- User interaction
- Routing
- Form validation
- API communication

Technology

- Angular
- Signals
- Reactive Forms

Must NOT contain

- Business rules
- Database logic

---

## API Layer

Responsible for

- Authentication
- Authorization
- Request validation
- Response formatting
- Exception handling

Must NOT contain

- Business logic

---

## Application Layer

Responsible for

- Business use cases
- Commands
- Queries
- DTO mapping
- Validation
- Transactions

Contains

- Services
- Validators
- DTOs

---

## Domain Layer

Responsible for

- Business rules
- Entities
- Value Objects
- Domain Events
- Interfaces

Must NOT depend on

- Entity Framework
- ASP.NET
- Angular

---

## Infrastructure Layer

Responsible for

- Database
- Email
- File Storage
- External APIs
- Logging
- Cache

Contains

- Repository implementations
- EF Core (`AutoPartDbContext`, migrations, entity configurations)
- External service clients (SMS/WhatsApp via Twilio, SMTP email)

---

# Dependency Rule

Dependencies always point inward.

```text
Presentation
      ↓
Application
      ↓
Domain

Infrastructure
      ↑
Implements interfaces defined by Domain/Application
```

The Domain layer should never reference outer layers.

---

# Feature-Based Organization

Each feature is self-contained.

```text
features/
│
├── products/
│   ├── pages/
│   ├── components/
│   ├── services/
│   ├── models/
│   ├── store/
│   └── routes.ts
│
├── customers/
│
└── purchases/
```

Benefits

- Easier maintenance
- Better scalability
- Independent development

---

# Request Flow

```text
Angular Component
        │
ProductApiService
        │
HTTP Request
        │
ProductsController
        │
Application Service / CQRS Handler
        │
Repository
        │
Database
```

Response follows the same path back to the UI.

---

# Dependency Injection

All dependencies are resolved through Dependency Injection.

Never instantiate services manually.

Good

```csharp
public ProductService(IProductRepository repository)
```

Bad

```csharp
var repository = new ProductRepository();
```

---

# Data Flow

```text
Database Entity
        │
Repository
        │
Application Layer
        │
DTO
        │
API
        │
Angular Model
        │
Component
```

Entities should never be exposed directly to the frontend.

---

# State Management

## Frontend

- Signals for local state
- Feature services for shared state
- No NgRx / external state library — do not introduce one

---

# Error Handling

Global exception middleware handles all unhandled exceptions.

API returns standardized error responses.

Frontend displays user-friendly messages.

---

# Authentication

Authentication

- JWT or Secure Cookies

Authorization

- Role-based
- Policy-based

---

# Logging

Log

- Errors
- Warnings
- Audit events
- Performance metrics

Never log

- Passwords
- Tokens
- Sensitive data

---

# Folder Structure (actual, this repo)

## Frontend

```text
src/AutoPartShop.WebApp/src/app/
├── features/        # admin, audit, dashboard, ecommerce, hr, inventory,
│                    # procurement, sales, warranty, …
├── layout/          # app shell (sidebar, topbar)
├── pages/           # top-level routed pages
├── shared/          # components, guards, interceptors, pipes, services, utils
├── app.routes.ts
└── app.config.ts
```

---

## Backend

```text
src/
├── AutoPartShop.Api/             # controllers, middleware, background services
├── AutoPartShop.Application/     # DTOs, service interfaces, use-case services
├── AutoPartShop.Domain/          # entities, repository interfaces
├── AutoPartShop.Infrastructure/  # DbContext, repositories, migrations
└── AutoPartShop.Api.Tests/       # backend tests
```

---

# Design Principles

Always follow

- SOLID
- DRY
- KISS
- YAGNI
- Separation of Concerns
- Dependency Inversion

---

# Scalability

The architecture should support

- New modules
- New APIs
- Multi-tenant support (if required)
- Background jobs
- Event-driven integration
- Horizontal scaling

---

# Security

- HTTPS only
- Input validation
- Authorization checks
- Secure secret management
- SQL injection protection
- XSS protection
- CSRF protection (where applicable)

---

# Testing Strategy

- Unit Tests
- Integration Tests
- End-to-End Tests

Business logic should be testable without the UI.

---

# Architecture Checklist

- Clean Architecture
- Feature-based modules
- Dependency Injection
- Layer separation
- DTO mapping
- Repository pattern
- Centralized error handling
- Authentication & Authorization
- Logging
- Validation
- Testability
- Scalability