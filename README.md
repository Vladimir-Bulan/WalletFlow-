# WalletFlow

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)
![Redis](https://img.shields.io/badge/Redis-7-red)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Message%20Broker-orange)
![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED)
![Architecture](https://img.shields.io/badge/Architecture-Microservices-black)
![Pattern](https://img.shields.io/badge/Pattern-CQRS%20%7C%20DDD%20%7C%20Event%20Sourcing-success)

Production-grade **microservices platform** for digital wallet management built with **.NET 8**, applying modern distributed systems and enterprise architecture patterns.

---

## What is WalletFlow?

WalletFlow is a backend platform that allows users to manage a digital wallet. With its APIs you can:

**As a user:**
- Register and automatically receive a JWT + Refresh Token
- Your wallet account is **created automatically** via integration event between microservices — no manual step needed
- Login with secure rotating token sessions
- **Deposit money** into your wallet
- **Withdraw money** from your wallet
- **Transfer money** to another account instantly
- Receive **real-time notifications** via SignalR every time your balance changes — deposits, withdrawals, or incoming transfers

**As a distributed system:**
- If a service crashes after saving data but before publishing the event, the **Outbox Pattern guarantees the message is not lost** and retried automatically
- Full transaction history is reconstructed from immutable events (**Event Sourcing**)
- The entire stack starts with a single command: `docker-compose up -d`

---

## Architecture

### System Overview
![Architecture Diagram](docs/architecture.png)
```
                    +──────────────+
                    |    Client    |
                    +──────┬───────+
                           |
                    +──────v────────+
                    |  API Gateway  |
                    |    (YARP)     |
                    |     :5000     |
                    +──────┬────────+
               +-----------+-----------+
               |                       |
        +──────v───────+       +──────v───────+
        |   Identity   |       |   Finance    |
        |     :5001    |       |     :5002    |
        +──────┬───────+       +──────┬───────+
               |                       |
               +-----------+-----------+
                           |
                     +─────v─────+
                     | RabbitMQ  |
                     +─────┬─────+
                           |
                +──────────+──────────+
                |                     |
          +─────v─────+         +─────v─────+
          | PostgreSQL|         |   Redis   |
          +-----------+         +-----------+
```

### CQRS + Outbox Internal Flow
![CQRS Diagram](docs/cqrs-outbox.png)
```
Command API -> Command Handler -> Aggregate Root -> Event Store
                                                 -> Outbox Table -> RabbitMQ
```

### Outbox Pattern Sequence
![Sequence Diagram](docs/sequence.png)
```
Client -> Gateway -> Finance -> DB (persist event)
                             -> Outbox (store integration event)
                                     -> RabbitMQ (publish message)
```

---

## Tech Stack

### Core
- .NET 8
- ASP.NET Core Minimal APIs
- Clean Architecture
- Domain-Driven Design (DDD)
- CQRS (MediatR)
- Event Sourcing

### Messaging
- MassTransit 8
- RabbitMQ
- Outbox Pattern

### Data
- Entity Framework Core 7
- PostgreSQL 16
- Redis 7 (Distributed Cache)

### Security
- JWT Bearer Authentication
- Refresh Tokens
- BCrypt password hashing

### Infrastructure
- YARP Reverse Proxy
- SignalR (real-time notifications)
- Docker + Docker Compose

---

## Quick Start
```bash
git clone https://github.com/Vladimir-Bulan/WalletFlow-.git
cd WalletFlow
docker-compose up -d
```

Services:

| Service      | URL |
|--------------|-----|
| Gateway      | http://localhost:5000 |
| Identity     | http://localhost:5001 |
| Finance      | http://localhost:5002 |
| RabbitMQ UI  | http://localhost:15672 (guest/guest) |

---

## API Examples

### Register
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"john@example.com","firstName":"John","lastName":"Doe","password":"Password123!"}'
```

> Registering automatically triggers a UserRegisteredIntegrationEvent via RabbitMQ. Finance service consumes it and creates the wallet account automatically.

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john@example.com","password":"Password123!"}'
```

### Deposit
```bash
curl -X POST http://localhost:5000/api/accounts/{id}/deposit \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"amount": 1000}'
```

### Transfer
```bash
curl -X POST http://localhost:5000/api/accounts/{id}/transfer \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"destinationAccountId": "...", "amount": 250}'
```

### Refresh Token
```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "..."}'
```

### Health Checks
```bash
curl http://localhost:5001/health
curl http://localhost:5001/health/ready
curl http://localhost:5002/health
curl http://localhost:5002/health/ready
```

### Real-time Notifications (SignalR)
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5002/hubs/wallet", {
    accessTokenFactory: () => accessToken
  }).build();

connection.on("BalanceUpdated", (data) => {
  console.log(`${data.type}: ${data.amount} ${data.currency}`);
  console.log(`New balance: ${data.newBalance}`);
});

await connection.start();
```

---

## Architectural Patterns

### Outbox Pattern
Ensures reliable at-least-once event delivery. Integration events are persisted atomically with domain state in the same DB transaction. A background processor publishes them to RabbitMQ, guaranteeing no message is lost even if the service crashes.

### Event Sourcing
Account state is never stored directly. Instead, every change (Deposit, Withdrawal, Transfer) is stored as an immutable domain event. State is reconstructed by replaying the event log.

### CQRS
Commands (write) and Queries (read) are fully separated via MediatR with validation pipelines using FluentValidation.

### Domain-Driven Design
- Aggregates (User, Account)
- Value Objects (Email, Money, AccountNumber, FullName)
- Domain Events (UserRegistered, MoneyDeposited, MoneyTransferred)
- Bounded Contexts (Identity, Finance)

---

## Project Structure
```
WalletFlow/
├── src/
│   ├── Gateway/
│   │   └── WalletFlow.Gateway/
│   ├── Services/
│   │   ├── Identity/
│   │   │   ├── Identity.Domain/
│   │   │   ├── Identity.Application/
│   │   │   ├── Identity.Infrastructure/
│   │   │   └── Identity.API/
│   │   └── Finance/
│   │       ├── Finance.Domain/
│   │       ├── Finance.Application/
│   │       ├── Finance.Infrastructure/
│   │       └── Finance.API/
│   └── Shared/
│       └── WalletFlow.Contracts/
├── tests/
│   ├── Identity.Tests/
│   └── Finance.Tests/
└── docker-compose.yml
```

---

## Running Tests
```bash
dotnet test
```

---

## Future Improvements

- Kubernetes deployment
- OpenTelemetry + distributed tracing
- Prometheus + Grafana monitoring
- Saga orchestration pattern
- Rate limiting
- API versioning

---

## Author

Vladimir Bulan
Backend Developer | .NET | Distributed Systems