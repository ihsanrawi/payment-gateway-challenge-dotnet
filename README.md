# Payment Gateway API

A .NET 8 Web API implementation of a payment gateway that processes card payments through a simulated acquiring bank, following Clean Architecture principles.

## Features

- Process card payments with idempotency support
- Three payment statuses: Authorized, Declined, Rejected
- PCI DSS compliant card number handling (only last 4 digits stored)
- In-memory storage (no database required)
- Comprehensive test coverage (50+ tests)

## Table of Contents

- [Overview](#overview)
- [Design Considerations](#design-considerations)
- [Assumptions Made](#assumptions-made)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [API Endpoints](#api-endpoints)
- [Payment Statuses](#payment-statuses)
- [Idempotency](#idempotency)
- [Testing](#testing)
- [Technologies](#technologies)

---

## Overview

This payment gateway API allows merchants to process card payments and retrieve payment details. It integrates with a simulated bank service that approves or declines transactions based on card number patterns.

### Payment Status Types

| Status | Description | Bank Called? |
|--------|-------------|--------------|
| **Authorized** | Payment was approved by the acquiring bank | Yes |
| **Declined** | Payment was declined by the acquiring bank | Yes |
| **Rejected** | Payment was rejected due to invalid request data | No |

---

## Design Considerations

I structured the solution using Clean Architecture principles so each part of the system has a clear responsibility and doesn't bleed into others. Controllers handle HTTP concerns, services orchestrate the payment flow, validators enforce input rules, and repositories deal with persistence. That way, if I change validation rules or swap out the bank provider, I'm not breaking unrelated parts of the system.

One thing I focused on was failing fast. Validation happens right at the start, before we ever call the bank. If something is wrong like a missing idempotency key, an invalid GUID, or bad request data, the controller returns immediately. That keeps the flow simple and avoids deeply nested conditionals. It also prevents wasting resources on unnecessary external calls.

The IBankClient abstraction keeps the bank integration cleanly separated. For testing, I can swap the real simulator for a mock or fake implementation without needing any live dependency. That makes unit tests fast and deterministic.

For this assessment, I kept persistence in-memory to keep things simple and focused on the core logic. Bank responses are mapped clearly to payment statuses, and unexpected errors default to a predictable rejected state so behaviour is consistent. I also made sure we never return full card numbers, only the last four digits to keep things secure by default.

In terms of testing, unit tests cover validation and service logic using a fake bank client, and integration tests verify the API works end-to-end. The overall structure also leaves room for future enhancements like supporting multiple bank providers or adding retry logic without needing a major redesign.

---

## Assumptions Made

- **Idempotency**: Minimal implementation without concurrency handling, key expiration, or distributed locking
- **Security**: API accepts unmasked card details instead of tokenized data (PCI DSS concern)
- **Observability**: Structured logging on critical flows only; not full observability
- **Resiliency**: No retry, circuit breaker, timeout policies, or rate limiting for bank API calls
- **Single Bank**: Only one acquiring bank supported; no multi-bank routing or failover
- **Minimal Testing**: Unit tests cover critical application flows only; no integration, performance or load tests
- **Monitoring**: No health check endpoint, metrics, distributed tracing, or alerting integration

---

## Project Structure

This solution follows **Clean Architecture** with clear separation of concerns:

```
.
├── src/
│   ├── PaymentGateway.Api/           # Presentation Layer
│   │   ├── Controllers/
│   │   │   └── PaymentsController.cs
│   │   ├── Validators/
│   │   │   └── PostPaymentRequestValidator.cs
│   │   ├── Middleware/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── PaymentGateway.Application/   # Application Layer
│   │   └── Services/
│   │       ├── IPaymentProcessorService.cs
│   │       └── PaymentProcessorService.cs
│   │
│   ├── PaymentGateway.Domain/        # Domain Layer
│   │   ├── Entities/
│   │   │   └── Payment.cs
│   │   ├── Models/
│   │   │   ├── PostPaymentRequest.cs
│   │   │   └── PostPaymentResponse.cs
│   │   ├── DTOs/
│   │   │   ├── BankRequestDto.cs
│   │   │   └── BankResponseDto.cs
│   │   ├── Enums/
│   │   │   └── PaymentStatus.cs
│   │   ├── Internal/
│   │   │   └── BankPaymentResult.cs
│   │   ├── Mappers/
│   │   │   ├── PaymentMappingProfile.cs
│   │   │   └── PaymentCreationMapper.cs
│   │   └── Configs/
│   │       └── BankSimulatorConfigs.cs
│   │
│   └── PaymentGateway.Infrastructure/ # Infrastructure Layer
│       ├── External/
│       │   ├── IBankClient.cs
│       │   └── BankClient.cs
│       └── Repository/
│           ├── IPaymentsRepository.cs
│           ├── PaymentsRepository.cs
│           ├── IIdempotencyRepository.cs
│           └── IdempotencyRepository.cs
│
└── test/
    ├── PaymentGateway.Api.Tests/
    │   ├── PaymentsControllerTests.cs
    │   └── PostPaymentRequestValidatorTests.cs
    │
    ├── PaymentGateway.Application.Tests/
    │   └── PaymentProcessorServiceTests.cs
    │
    └── PaymentGateway.Infrastructure.Tests/
        ├── BankClientTests.cs
        ├── PaymentsRepositoryTests.cs
        ├── IdempotencyRepositoryTests.cs
        └── Helpers/
            └── MockHttpMessageHandler.cs
```

---

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker (for bank simulator)

### Start the Bank Simulator

```bash
docker-compose up
```

The simulator will be available at `http://localhost:8080`

### Run the API

```bash
cd src/PaymentGateway.Api
dotnet run
```

The API will be available at `https://localhost:7092`

### Swagger UI

When running in Development mode:
```
https://localhost:7092/swagger
```

### Test with HTTP File

You can also test the API using the provided `Payments.http` file in the root directory. It includes pre-configured requests for:

- Authorized payment (card ending in odd number)
- Declined payment (card ending in even number)
- Rejected payment (invalid CVV)
- Idempotency test scenarios
- Payment retrieval

Open `Payments.http` in VS Code or your preferred IDE to send requests directly.

---

## API Endpoints

### Process a Payment

```
POST /api/payments
Headers:
  Idempotency-Key: {guid}

Body:
{
  "cardNumber": "12345678901234",
  "expiryMonth": 12,
  "expiryYear": 2026,
  "currency": "GBP",
  "amount": 10050,
  "cvv": "123"
}
```

**Response (200 OK):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Authorized",
  "cardNumberLastFour": 1234,
  "expiryMonth": 12,
  "expiryYear": 2026,
  "currency": "GBP",
  "amount": 10050
}
```

### Retrieve Payment Details

```
GET /api/payments/{id}
```

**Response (200 OK):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Authorized",
  "cardNumberLastFour": 1234,
  "expiryMonth": 12,
  "expiryYear": 2026,
  "currency": "GBP",
  "amount": 10050
}
```

---

## Payment Statuses

| Status | Description | Bank Called? |
|--------|-------------|--------------|
| **Authorized** | Payment approved by acquiring bank | Yes |
| **Declined** | Payment declined by acquiring bank | Yes |
| **Rejected** | Invalid request data (validation failed) | No |

### Bank Simulator Behavior

The simulator responds based on the last digit of the card number:

| Card Ending | Response |
|-------------|----------|
| **Odd (1,3,5,7,9)** | Authorized |
| **Even (2,4,6,8)** | Declined |
| **Zero (0)** | 503 Service Unavailable |

---

## Idempotency

Clients must provide an `Idempotency-Key` header (GUID format) to prevent duplicate payment processing.

### How It Works

1. **First Request**: Creates a new payment, stores the idempotency key → payment ID mapping
2. **Duplicate Request**: Returns the cached payment without calling the bank again
3. **Rejected Requests**: Key is NOT locked, allowing retry with corrected data

### Example

```bash
# First request - creates payment
POST /api/payments
Idempotency-Key: 11111111-1111-1111-1111-111111111111

# Second request - returns cached payment (no bank call)
POST /api/payments
Idempotency-Key: 11111111-1111-1111-1111-111111111111
```

---

## Testing

```bash
dotnet test
```

### Test Coverage

| Project | Tests | Coverage |
|---------|-------|----------|
| PaymentGateway.Api.Tests | 20 | Controllers, Validators |
| PaymentGateway.Application.Tests | 10 | Service layer |
| PaymentGateway.Infrastructure.Tests | 20 | Repositories, Bank Client |
| **Total** | **50** | All passing |